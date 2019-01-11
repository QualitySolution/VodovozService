#!/bin/bash
echo "Which version to upload?"
echo "1) Release"
echo "2) Debug"
read case;

ssh root@vod-srv.qsolution.ru "systemctl stop vodovozservice"

#Удаляем старый лог.
#ssh root@vod-srv.qsolution.ru "sudo rm /var/log/VodovozService/server.log"

case $case in
    1)
rsync -vizaP --delete ./bin/Release/ root@vod-srv.qsolution.ru:/opt/VodovozService
;;
    2)
rsync -vizaP --delete ./bin/Debug/ root@vod-srv.qsolution.ru:/opt/VodovozService
;;
esac

ssh root@vod-srv.qsolution.ru "sudo systemctl start vodovozservice"
