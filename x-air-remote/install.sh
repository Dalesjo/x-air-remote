#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

cat <<EOF > /lib/systemd/system/x-air-remote.service
[Unit]
Description=.NET Core Console Application

[Service]
WorkingDirectory=$DIR
ExecStart=$DIR/x-air-remote
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=x-air
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target 
EOF

chmod +x $DIR/x-air-remote

systemctl daemon-reload 
systemctl enable x-air-remote.service

# Start service
systemctl start x-air-remote.service

# View service status
systemctl status x-air-remote.service 
