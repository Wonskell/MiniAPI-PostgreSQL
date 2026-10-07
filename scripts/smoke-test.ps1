param([string]$BaseUrl = 'http://localhost:5000')
$ErrorActionPreference = 'Stop'
$created = [System.Collections.Generic.List[string]]::new()
$checks = 0

function Assert-That($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}

function Request([string]$Method, [string]$Route, $Body = $null) {
    $parameters = @{ Uri = "$BaseUrl$Route"; Method = $Method; UseBasicParsing = $true }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json; charset=utf-8'
        $parameters.Body = [Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json))
    }
    Invoke-WebRequest @parameters
}

function Assert-Status([int]$Expected, [string]$Method, [string]$Route, $Body = $null) {
    $actual = 0
    try { $actual = [int](Request $Method $Route $Body).StatusCode }
    catch {
        if ($null -eq $_.Exception.Response) { throw }
        $actual = [int]$_.Exception.Response.StatusCode
    }
    Assert-That ($actual -eq $Expected) "$Method $Route returned $actual instead of $Expected"
}

try {
    $departments = (Request GET '/departments').Content | ConvertFrom-Json
    Assert-That ($departments.Count -ge 3) 'Expected the initial departments'
    $marker = 'check-' + [guid]::NewGuid().ToString('N')
    $specialName = "$marker O'Neil <script>"
    $response = Request POST '/user' @{ name = "  $specialName  "; age = 0; departmentId = $departments[0].id }
    $user = $response.Content | ConvertFrom-Json
    $created.Add($user.id)
    Assert-That ($response.StatusCode -eq 201) 'POST should return 201'
    Assert-That ($response.Headers.Location -like "*/user/$($user.id)") 'Missing Location header'
    Assert-That ($user.name -ceq $specialName) 'Name should be trimmed without changing its content'
    Assert-That ($user.departmentName -eq $departments[0].name) 'JOIN did not return department name'
    $read = (Request GET "/user/$($user.id)").Content | ConvertFrom-Json
    Assert-That ($read.id -eq $user.id) 'Cannot read the created user'
    $found = (Request GET ('/user/search?name=' + [uri]::EscapeDataString($specialName.ToUpperInvariant()))).Content | ConvertFrom-Json
    Assert-That ($found.id -eq $user.id) 'Case-insensitive exact search failed'
    $filtered = (Request GET '/user/filter?minAge=0&maxAge=0').Content | ConvertFrom-Json
    Assert-That (@($filtered | Where-Object id -eq $user.id).Count -eq 1) 'Inclusive age filter failed'
    $second = (Request POST '/user' @{ name = "$marker-max"; age = 150 }).Content | ConvertFrom-Json
    $created.Add($second.id)
    Assert-That ($null -eq $second.departmentId) 'Department should be optional'
    $sorted = @((Request GET '/user/sorted').Content | ConvertFrom-Json)
    for ($i = 1; $i -lt $sorted.Count; $i++) {
        Assert-That ($sorted[$i - 1].age -le $sorted[$i].age) 'Age sorting failed'
    }
    $recent = @((Request GET '/user/recent?count=1').Content | ConvertFrom-Json)
    Assert-That ($recent.Count -eq 1) 'Recent limit failed'
    $stats = (Request GET '/user/stats').Content | ConvertFrom-Json
    Assert-That ($stats.totalUsers -ge 2) 'Statistics did not include created users'
    $groups = (Request GET '/departments/stats').Content | ConvertFrom-Json
    $group = $groups | Where-Object id -eq $departments[0].id
    Assert-That ($group.userCount -ge 1) 'Department statistics failed'

    Assert-Status 400 POST '/user' @{ name = ' '; age = 20 }
    Assert-Status 400 POST '/user' @{ name = ('a' * 101); age = 20 }
    Assert-Status 400 POST '/user' @{ name = 'Invalid'; age = -1 }
    Assert-Status 400 POST '/user' @{ name = 'Invalid'; age = 151 }
    Assert-Status 400 POST '/user' @{ name = 'Invalid' }
    Assert-Status 400 POST '/user' @{ name = 'Invalid'; age = 20; departmentId = -1 }
    Assert-Status 400 POST '/user' @{ name = 'Invalid'; age = 20; departmentId = 2147483647 }
    Assert-Status 400 GET '/user/filter?minAge=30&maxAge=18'
    Assert-Status 400 GET '/user/filter?minAge=-1'
    Assert-Status 400 GET '/user/recent?count=0'
    Assert-Status 400 GET '/user/recent?count=101'
    Assert-Status 404 GET ('/user/search?name=' + $marker + '-missing')
    Assert-Status 404 GET ('/user/' + [guid]::NewGuid())
    Assert-Status 204 DELETE "/user/$($user.id)"
    Assert-Status 404 DELETE "/user/$($user.id)"
    Assert-Status 404 GET "/user/$($user.id)"
    Assert-Status 200 GET '/'
    Assert-Status 200 GET '/dashboard.html'
    Assert-Status 200 GET '/download.html'
    Write-Output "Passed $checks checks."
}
finally {
    foreach ($id in $created) {
        try { Request DELETE "/user/$id" | Out-Null }
        catch {
            if ($null -eq $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 404) {
                Write-Warning "Could not remove test user $id"
            }
        }
    }
}
