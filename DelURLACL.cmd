@Echo Off
@Echo Deletes permissions for URL reservation
@Echo Parameter 1 "%1" == 7070
@Echo Parameter 2 "%2" == localhost
pause
netsh http delete urlacl url=http://+:%1/%2

@Echo Off
@Echo Deletes permissions for URL reservation
@Echo Parameter 1 "%1" == 5401
@Echo Parameter 2 "%2" == localhost
pause
netsh http delete urlacl url=http://+:%1/%2