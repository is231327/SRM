from flask import Flask, jsonify, request
import json
import threading
import random
import time
import os
import requests
import string
import sys

app = Flask(__name__)

def generate_device_id():
    prefix = 'shellydw2-'
    suffix = ''.join(random.choice(string.ascii_lowercase + string.digits) for _ in range(6))
    return prefix + suffix

DEVICE_ID = generate_device_id()

def set_nested_value(data, path, value):
    current = data
    for part in path[:-1]:
        if isinstance(current, list):
            part = int(part)
        if isinstance(current, dict) and part not in current:
            current[part] = {} if not part.isdigit() else []
        current = current[part]
    current[path[-1]] = value

validation_schema = {
    'dark_threshold': {'type': 'number', 'min': 0, 'max': 10000, 'path': ['dark_threshold']},
    'twilight_threshold': {'type': 'number', 'min': 0, 'max': 10000, 'path': ['twilight_threshold']},
    'led_status_disable': {'type': 'bool', 'path': ['led_status_disable']},
    'lux_wakeup_enable': {'type': 'bool', 'path': ['lux_wakeup_enable']},
    'reverse_open_close': {'type': 'bool', 'path': ['reverse_open_close']},
    'tilt_enabled': {'type': 'bool', 'path': ['tilt_enabled']},
    'vibration_enabled': {'type': 'bool', 'path': ['vibration_enabled']},
    'vibration_sensitivity': {'type': 'number', 'min': 0, 'max': 100, 'path': ['vibration_sensitivity']},
    'sleep_mode_period': {'type': 'number', 'min': 0, 'path': ['sleep_mode', 'period']},
    'sleep_mode_unit': {'type': 'string', 'allowed': ['h'], 'path': ['sleep_mode', 'unit']},
    'temperature_threshold': {'type': 'number', 'path': ['sensors', 'temperature_threshold']},
    'temperature_unit': {'type': 'string', 'allowed': ['C', 'F'], 'path': ['sensors', 'temperature_unit']},
    'temperature_offset': {'type': 'number', 'path': ['temperature_offset']},
    'device.sleep_mode': {'type': 'bool', 'path': ['device', 'sleep_mode']}
}

actions_validation_schema = {
    'enabled': {'type': 'bool', 'actions': ['report_url', 'dark_url', 'twilight_url', 'open_url', 'close_url', 'vibration_url', 'temp_over_url', 'temp_under_url']},
    'urls': {'type': 'list', 'item_type': 'string', 'actions': ['report_url', 'dark_url', 'twilight_url', 'open_url', 'close_url', 'vibration_url', 'temp_over_url', 'temp_under_url']},
    'dark_threshold': {'type': 'number', 'min': 0, 'max': 10000, 'actions': ['dark_url']},
    'twilight_threshold': {'type': 'number', 'min': 0, 'max': 10000, 'actions': ['twilight_url']},
    'temp_over_value': {'type': 'number', 'actions': ['temp_over_url']},
    'temp_over_onetime': {'type': 'bool', 'actions': ['temp_over_url']},
    'temp_under_value': {'type': 'number', 'actions': ['temp_under_url']},
    'temp_under_onetime': {'type': 'bool', 'actions': ['temp_under_url']}
}

@app.route('/', methods=['GET'])
def hello():
    return app.send_static_file("hello.html")

@app.route('/settings', methods=['GET'])
def settings():
    query_params = request.args.to_dict()
    if query_params:
        errors = {}
        for key, value_str in query_params.items():
            if key not in validation_schema:
                errors[key] = "Invalid parameter"
                continue
            rules = validation_schema[key]
            try:
                if rules['type'] == 'bool':
                    if value_str.lower() not in ('true', 'false'):
                        errors[key] = "Must be 'true' or 'false'"
                        continue
                    value = value_str.lower() == 'true'
                elif rules['type'] == 'number':
                    value = float(value_str)
                else:
                    value = value_str.strip()
            except ValueError as e:
                errors[key] = f"Invalid value for type {rules['type']}: {str(e)}"
                continue
            if 'min' in rules and value < rules['min']:
                errors[key] = f"Must be at least {rules['min']}"
            if 'max' in rules and value > rules['max']:
                errors[key] = f"Must be at most {rules['max']}"
            if rules['type'] == 'string' and 'allowed' in rules and value not in rules['allowed']:
                errors[key] = f"Must be one of {rules['allowed']}"
        dark = None
        twilight = None
        if 'dark_threshold' in query_params:
            try: dark = float(query_params['dark_threshold'])
            except: pass
        if 'twilight_threshold' in query_params:
            try: twilight = float(query_params['twilight_threshold'])
            except: pass
        if dark is not None and twilight is not None and dark >= twilight:
            errors['dark_threshold']   = "Must be less than twilight_threshold"
            errors['twilight_threshold'] = "Must be greater than dark_threshold"
        if errors:
            return jsonify({"errors": errors}), 400
        with open("settings.json", "r") as f:
            data = json.load(f)
        for key, value_str in query_params.items():
            rules = validation_schema[key]
            if rules['type'] == 'bool': value = value_str.lower() == 'true'
            elif rules['type'] == 'number': value = float(value_str)
            else: value = value_str.strip()
            set_nested_value(data, rules['path'], value)
        with open("settings.json", "w") as f:
            json.dump(data, f, indent=4)
        return ""
    else:
        with open("settings.json") as f:
            data = json.load(f)
        data["id"] = DEVICE_ID
        return jsonify(data)

@app.route('/settings/actions', methods=['GET'])
def actions():
    query_params = request.args.to_dict()
    if query_params:
        errors = {}
        with open("settings_actions.json", "r") as f:
            data = json.load(f)
        for param, value_str in query_params.items():
            parts = param.split('_')
            action = None
            field = None
            for i in range(len(parts), 0, -1):
                candidate = '_'.join(parts[:i])
                if candidate in data.get('actions', {}):
                    action = candidate
                    field  = '_'.join(parts[i:]) if i < len(parts) else None
                    break
            if not action or not field:
                errors[param] = "Invalid parameter"
                continue
            if field not in actions_validation_schema:
                errors[param] = f"Invalid field '{field}' for action '{action}'"
                continue
            rules = actions_validation_schema[field]
            if action not in rules.get('actions', []):
                errors[param] = f"Field '{field}' not allowed for action '{action}'"
                continue
            try:
                if rules['type'] == 'bool':
                    if value_str.lower() not in ('true','false'):
                        errors[param] = "Must be 'true' or 'false'"
                        continue
                    value = value_str.lower() == 'true'
                elif rules['type'] == 'number': value = float(value_str)
                elif rules['type'] == 'list':   value = [v.strip() for v in value_str.split(',')] if value_str else []
                else:                           value = value_str.strip()
            except ValueError as e:
                errors[param] = f"Invalid value for type {rules['type']}: {str(e)}"
                continue
            if 'min' in rules and value < rules['min']:
                errors[param] = f"Must be at least {rules['min']}"
            if 'max' in rules and value > rules['max']:
                errors[param] = f"Must be at most {rules['max']}"
            set_nested_value(data, ['actions', action, 0, field], value)
        if errors:
            return jsonify({"errors": errors}), 400
        with open("settings_actions.json", "w") as f:
            json.dump(data, f, indent=4)
        return ""
    else:
        with open("settings_actions.json") as f:
            data = json.load(f)
        data["id"] = DEVICE_ID
        return jsonify(data)

status_lock = threading.Lock()
settings_lock = threading.Lock()

def generate_status_data():
    print("[DEBUG] Simulator loop entered", flush=True)

    try:
        with open("status.json") as f:
            existing = json.load(f)
        current_sensor_state = existing.get("sensor", {}).get("state", "close")
        current_bat_value = float(existing.get("bat", {}).get("value", 100))
    except:
        current_sensor_state = "close"
        current_bat_value = 100.0

    while True:
        new_data = {"id": DEVICE_ID, "is_valid": True, "act_reasons": ["periodic"]}
        with settings_lock:
            try:
                with open("settings.json","r") as f: settings = json.load(f)
                dark_th = settings.get('dark_threshold',100)
                twi_th  = settings.get('twilight_threshold',200)
                temp_unit = settings.get('temperature_unit','C')
            except:
                dark_th = 100; twi_th = 200; temp_unit = 'C'
        tC = round(random.uniform(5.0,35.0),1)
        tF = round(tC*9/5+32,2)
        new_data["tmp"] = {"value": tC if temp_unit=='C' else tF, "units": temp_unit, "tC": tC, "tF": tF, "is_valid": True}
        lux = random.randint(0,1500)
        illum = 'dark' if lux<dark_th else 'twilight' if lux<twi_th else 'light'
        new_data["lux"]   = {"value": lux, "illumination": illum, "is_valid": True}
        vibration_val = 1 if random.random()<0.1 else 0
        tilt_val      = random.randint(0,90)
        new_data["accel"] = {"tilt": tilt_val, "vibration": vibration_val, "vibration_time": 60}
        if random.random() < 0.1:
            current_sensor_state = 'open' if current_sensor_state == 'close' else 'close'

        new_data["sensor"] = {"state": current_sensor_state, "is_valid": True}

        current_bat_value = max(0, current_bat_value - random.uniform(0.1,0.5))
        new_data["bat"] = {"value": int(round(current_bat_value)), "voltage": round(current_bat_value*0.03845,2)}

        try:
            with open("settings_actions.json","r") as f: actions_conf = json.load(f)
            triggered = {}
            for action, slots in actions_conf.get("actions",{}).items():
                cfg = slots[0]
                if not cfg.get("enabled", False): 
                    continue
                send = False; val = None

                if action == "report_url":
                    send = True
                    val = None
                elif action == "dark_url" and new_data["lux"]["illumination"]=="dark":
                    send, val = True, new_data["lux"]["value"]
                elif action == "twilight_url" and new_data["lux"]["illumination"]=="twilight":
                    send, val = True, new_data["lux"]["value"]
                elif action == "open_url" and new_data["sensor"]["state"]=="open":
                    send, val = True, "open"
                elif action == "close_url" and new_data["sensor"]["state"]=="close":
                    send, val = True, "close"
                elif action == "vibration_url" and new_data["accel"]["vibration"]==1:
                    send, val = True, 1
                elif action == "temp_over_url":
                    thr = cfg.get("temp_over_value")
                    if thr is not None and new_data["tmp"]["value"]>thr:
                        send, val = True, new_data["tmp"]["value"]
                elif action == "temp_under_url":
                    thr = cfg.get("temp_under_value")
                    if thr is not None and new_data["tmp"]["value"]<thr:
                        send, val = True, new_data["tmp"]["value"]

                if send:
                    print(f"[DEBUG] Action triggered: {action}, value: {val}")
                    params = {
                        "state": new_data["sensor"]["state"],
                        "lux": new_data["lux"]["value"],
                        "temp": f"{new_data['tmp']['value']:.2f}",
                        "tilt": new_data["accel"]["tilt"],
                        "vibration": new_data["accel"]["vibration"],
                        "id": DEVICE_ID
                    }
                    for url in cfg.get("urls", []):
                        #print(f"[DEBUG] Calling URL: {url} with params: {params}")
                        try:
                            requests.get(url, params=params, timeout=5)
                        except:
                            pass
                    triggered[action] = val

            if triggered:
                print(f"[DEBUG] Actions triggered summary: {triggered}")
                new_data["actions_triggered"] = triggered
        except:
            pass
        with status_lock:
            with open("status.json","w") as f:
                json.dump(new_data,f,indent=4)
        sleep_time = random.randint(240,360)
        #print(f"[DEBUG] Sleeping {sleep_time}s before next generation", flush=True)
        time.sleep(sleep_time)
        #print(f"[DEBUG] Status data updated: {new_data}", flush=True)

thread = threading.Thread(target=generate_status_data, daemon=True)
thread.start()
#print("[DEBUG] Simulator thread started after function definition", flush=True)

@app.route('/status', methods=['GET'])
def status():
    with status_lock:
        with open("status.json") as f: data = json.load(f)
    data["id"] = DEVICE_ID
    return jsonify(data)
