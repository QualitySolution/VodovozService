#!/bin/bash
echo "Which version to upload?"
echo "1) Release"
echo "2) Debug"
read case;

ssh root@vod-srv.qsolution.ru "systemctl stop vodovozservice"

case $case in
    1)
rsync -vizaP ./bin/Release/ root@vod-srv.qsolution.ru:/opt/VodovozService
;;
    2)
rsync -vizaP ./bin/Debug/ root@vod-srv.qsolution.ru:/opt/VodovozService
;;
esac

ssh root@saas.qsolution.ru "sudo systemctl start vodovozservice"
