#!/bin/bash

echo "Какие службы необходимо обновить?"
echo "1) Driver"
echo "2) Email"
echo "3) Mobile"
echo "4) OSM"
echo "5) SmsInformer"
echo "6) ModulKassa (SalesReceipts)"
echo "0) Old server DriverMobileGroup"
echo "Можно вызывать вместе, например Driver+Email=12"
read service;

echo "Какую сборку использовать?"
echo "1) Release"
echo "2) Debug"
read build;

driverServiceFolder="VodovozAndroidDriverService"
driverServiceName="vodovoz-driver.service"

emailServiceFolder="VodovozEmailService"
emailServiceName="vodovoz-email.service"

mobileServiceFolder="VodovozMobileService"
mobileServiceName="vodovoz-mobile.service"

osmServiceFolder="VodovozOSMService"
osmServiceName="vodovoz-osm.service"

smsServiceFolder="VodovozSmsInformerService"
smsServiceName="vodovoz-smsinformer.service"

kassaServiceFolder="VodovozSalesReceiptsService"
kassaServiceName="vodovoz-sales-receipts.service"

serverAddress="root@srv2.vod.qsolution.ru"
serverPort="2203"


# ------ для объединенной службы водителей и мобильного приложения для старого сервера

oldServerAddress="root@192.168.0.7"
oldServerPort="22"

driverMobileGroupServiceFolder="VodovozDriverAndMobileServiceGroup"
driverMobileGroupServiceName="vodovoz-driver-mobile-group.service"

# ------------------------------

buildFolderName=""
case $build in
	1)
		buildFolderName="Release"
	;;
	2)
		buildFolderName="Debug"
	;;
esac

function DeleteHttpDll {
	deletedFilePath="./$1/bin/$buildFolderName/System.Net.Http.dll"

	echo "-- Delete incorrect generated files: $deletedFilePath"

	if [ -f $deletedFilePath ]
		then rm $deletedFilePath
	fi
}

function CopyFiles {
	rsync -vizaP --delete -e "ssh -p $serverPort" ./$1/bin/$buildFolderName/ $serverAddress:/opt/$1
}

function CopyFilesToOldServer {
	rsync -vizaP --delete -e "ssh -p $oldServerPort" ./$1/bin/$buildFolderName/ $oldServerAddress:/opt/$1
}

function UpdateDriverService {
	printf "\nОбновление службы водителей\n"

	echo "-- Stoping $driverServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl stop $driverServiceName

	echo "-- Copying $driverServiceName files"
	DeleteHttpDll $driverServiceFolder
	CopyFiles $driverServiceFolder

	echo "-- Starting $driverServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl start $driverServiceName
}

function UpdateEmailService {
	printf "\nОбновление службы отправки электронной почты\n"

	echo "-- Stoping $emailServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl stop $emailServiceName	

	echo "-- Copying $emailServiceName files"
	DeleteHttpDll $emailServiceFolder
	CopyFiles $emailServiceFolder

	echo "-- Starting $emailServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl start $emailServiceName
}

function UpdateMobileService {
	printf "\nОбновление службы мобильного приложения\n"

	echo "-- Stoping $mobileServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl stop $mobileServiceName	

	echo "-- Copying $mobileServiceName files"
	DeleteHttpDll $mobileServiceFolder
	CopyFiles $mobileServiceFolder

	echo "-- Starting $mobileServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl start $mobileServiceName
}

function UpdateOSMService {
	printf "\nОбновление службы OSM\n"

	echo "-- Stoping $osmServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl stop $osmServiceName

	echo "-- Copying $osmServiceName files"
	DeleteHttpDll $osmServiceFolder
	CopyFiles $osmServiceFolder

	echo "-- Starting $osmServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl start $osmServiceName
}

function UpdateSMSInformerService {
	printf "\nОбновление службы SMS информирования\n"

	echo "-- Stoping $smsServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl stop $smsServiceName

	echo "-- Copying $smsServiceName files"
	DeleteHttpDll $smsServiceFolder
	CopyFiles $smsServiceFolder

	echo "-- Starting $smsServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl start $smsServiceName
}

function UpdateSalesReceiptsService {
	printf "\nОбновление службы управления кассовым апаратом\n"

	echo "-- Stoping $kassaServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl stop $kassaServiceName

	echo "-- Copying $kassaServiceName files"
	DeleteHttpDll $kassaServiceFolder
	CopyFiles $kassaServiceFolder

	echo "-- Starting $kassaServiceName"
	ssh $serverAddress -p$serverPort sudo systemctl start $kassaServiceName
}

function UpdateDriverMobileGroupService {
	printf "\nОбновление службы для водителей и мобильного приложения на старом сервере\n"

	echo "-- Stoping $driverMobileGroupServiceName"
	ssh $oldServerAddress -p$oldServerPort sudo systemctl stop $driverMobileGroupServiceName

	echo "-- Copying $driverMobileGroupServiceName files"
	DeleteHttpDll $driverMobileGroupServiceFolder
	CopyFilesToOldServer $driverMobileGroupServiceFolder

	echo "-- Starting $driverMobileGroupServiceName"
	ssh $oldServerAddress -p$oldServerPort sudo systemctl start $driverMobileGroupServiceName
}

case $service in
	*1*)
		UpdateDriverService
	;;&
	*2*)
		UpdateEmailService
	;;&
	*3*)
		UpdateMobileService
	;;&
	*4*)
		UpdateOSMService
	;;&
	*5*)
		UpdateSMSInformerService
	;;&
	*6*)
		UpdateSalesReceiptsService
	;;&
	*0*)
		UpdateDriverMobileGroupService
	;;
esac

read -p "Press enter to exit"
