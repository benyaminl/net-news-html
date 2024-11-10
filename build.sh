#!/bin/bash
podman build . -t localhost/net-news-html
systemctl --user restart net-news
podman images --format "{{.ID}} {{.Tag}}" | grep none | cut -d ' ' -f 1 | xargs -I {} podman rmi {}