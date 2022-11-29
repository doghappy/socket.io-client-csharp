$ports = 11400, 11410, 11401, 11411, 11200, 11210, 11201, 11211
$retry = 10
$delay = 3
function Test-SocketIOConnection($Port) {
    for ($i = 1; $i -le $retry; $i++) {
        $result = Test-Connection -TargetName 127.0.0.1 -TcpPort $Port
        if ($result) {
            Write-Host "$Port opened"
            return $true
        }
        else {
            Write-Host "$Port not open, will retry($i) after $($delay)s ..."
            Start-Sleep -Seconds $delay
        }
    }
    Write-Host "$port is not open after $retry retries."
    return $false
}

foreach ($port in $ports) {
    $result = Test-SocketIOConnection -Port $port
    if (!$result) {
        throw "All socket.io erver ports must be open, but port $port is closed"
    }
}