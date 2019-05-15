#!/bin/bash
systemctl stop x-air-remote.service
systemctl disable x-air-remote.service
rm /lib/systemd/system/x-air-remote.service -f
systemctl daemon-reload 