#!/bin/bash

# Ensure the SQLite file has proper permissions
chmod 666 ~/Code/net-news-html/what.sqlite

# Build and restart the container
podman build . -t localhost/net-news-html
systemctl --user restart net-news

# Clean up unused images
podman images --format "{{.ID}} {{.Tag}}" | grep none | cut -d ' ' -f 1 | xargs -I {} podman rmi {}