Setting.rest_api_enabled = '1'

admin = User.find(1)
admin.login = ENV.fetch('SRM_REDMINE_ADMIN_USERNAME')
admin.password = ENV.fetch('SRM_REDMINE_ADMIN_PASSWORD')
admin.password_confirmation = ENV.fetch('SRM_REDMINE_ADMIN_PASSWORD')
admin.must_change_passwd = false
admin.save!

identifier = ENV.fetch('SRM_REDMINE_PROJECT_IDENTIFIER')
project = Project.find_or_initialize_by(identifier: identifier)
project.name = identifier
project.is_public = false
project.save!
project.trackers = Tracker.all

token = Token.where(user: admin, action: 'api').first_or_create!
puts "SRM_TOKEN=#{token.value}"
