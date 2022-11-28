$ports = 11400, 11410, 11401, 11411, 11200, 11210, 11201, 11211
function Test-SocketIOConnection($Port) {
    for ($i = 0; $i -lt 3; $i++) {
        $result = Test-Connection -TargetName localhost -TcpPort $Port
        if ($result) {
            Write-Host "$Port opened"
            return $true
        }
        else {
            Write-Host "$Port not open, will retry($i) after 3 s..."
            Start-Sleep -Seconds 3
        }
    }
    Write-Host "$port is not open after 3 retries."
    return $false
}

foreach ($port in $ports) {
    $result = Test-SocketIOConnection -Port $port
    if (!$result) {
        throw "All socket.io erver ports must be open, but port $port is closed"
    }
}