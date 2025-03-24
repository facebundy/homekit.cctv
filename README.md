# RabbitMQ Installation and Configuration

6

- check Erlang compatability with RabbitMQ

** Installtion

Install Erlang
Install Rabbit_MQ
Set system variables
Set Environment variable-

RABBITMQ_BASE c:\RabbitMQ Server
ERLANG_HOME C:\Program Files\erl10.7
Config MQ Server

stop RabbitMq : rabbitmq-service.bat stop
Enable management : rabbitmq-plugins.bat enable rabbitmq_management
Reinstall server : rabbitmq-service.bat install
Start Server : rabbitmq-service.bat start
Start App : rabbitmqctl.bat start_app
Open Brower : http://localhost:15672 user guest / guest
Add new User

List users : rabbitmqctl.bat list_users

Add new user : rabbitmqctl.bat add_user user1 pass1

Give administrator privileges : rabbitmqctl.bat set_user_tags user1


# Install service
1. Open cmd in administrator mode.
2. Run the command:
`sc create "Homekit CCTV" binpath="C:\Users\westrada\Documents\repositories\garage\homekit.cctv\bin\Release\net9.0\win-x64\publish\homekit.cctv.exe"`