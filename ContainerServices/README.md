# Container Services
Build docker stuff here<br>
Example:

## IssueTracker
```
  redmine:
    image: redmine
    container_name: redmine
    restart: always
    ports:
      - 3000:3000
    links:
      - dbmysql
    environment:
      REDMINE_DB_MYSQL: dbmysql
      REDMINE_DB_PASSWORD: example
      REDMINE_SECRET_KEY_BASE: supersecretkey
```

#### MySQL for Redmine
```
  dbmysql:
    image: mysql:5.7
    container_name: dbmysql
    restart: always
    volumes:
      - c:/Docker/Database/mysql/:/var/lib/mysql
    environment:
      MYSQL_ROOT_PASSWORD: example
      MYSQL_DATABASE: redmine
```

## RDBMS
```
  dbsqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: dbmssqlserver
    restart: always
    ports:
        - 1433:1433
    volumes:
        - c:/docker/Database/mssql:/var/opt/mssql/data
    environment:
        SA_PASSWORD: "password123!"
        ACCEPT_EULA: "Y"
```

## NoSQL
```
  dbredis:
     container_name: dbredis
     restart: always
     ports:
         - 6379:6379
     image: redis
```
