param(
    [string]$PokemonDataPath = "Assets/Game/Resources/PokemonData"
)

$ErrorActionPreference = "Stop"

function Normalize-PokemonName {
    param([string]$Name)

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return ""
    }

    $normalized = $Name.ToLowerInvariant()
    $normalized = $normalized -replace "\\u2019", ""
    $normalized = $normalized -replace "[’'`"]", ""
    $normalized = $normalized -replace "[^a-z0-9]", ""

    switch ($normalized) {
        "farfetchd" { return "farfetchd" }
        "mrmime" { return "mrmime" }
        "hooh" { return "hooh" }
        "nidoranf" { return "nidoranf" }
        "nidoranm" { return "nidoranm" }
        "charmeleon" { return "charmeleon" }
        "charmaleon" { return "charmeleon" }
        "dzoowe" { return "drowzee" }
        "steelnix" { return "steelix" }
        default { return $normalized }
    }
}

function Add-Encounter {
    param(
        [hashtable]$Map,
        [string]$Location,
        [string[]]$Names
    )

    foreach ($name in $Names) {
        $key = Normalize-PokemonName $name
        if ([string]::IsNullOrWhiteSpace($key)) {
            continue
        }

        if (-not $Map.ContainsKey($key)) {
            $Map[$key] = [System.Collections.Generic.List[string]]::new()
        }

        if (-not $Map[$key].Contains($Location)) {
            $Map[$key].Add($Location)
        }
    }
}

function Get-AssetGuid {
    param([string]$AssetPath)

    $metaPath = "$AssetPath.meta"
    if (-not (Test-Path -LiteralPath $metaPath)) {
        return $null
    }

    $meta = Get-Content -LiteralPath $metaPath -Raw
    if ($meta -match "guid:\s*([0-9a-f]+)") {
        return $Matches[1]
    }

    return $null
}

function Get-PokemonName {
    param([string]$Text)

    if ($Text -match "(?m)^\s*pokemonName:\s*(.+?)\s*$") {
        return $Matches[1].Trim()
    }

    return $null
}

function Set-EncounterLocations {
    param(
        [string]$Text,
        [string[]]$Locations
    )

    if ($Locations -and $Locations.Count -gt 0) {
        $locationLines = ($Locations | ForEach-Object { "  - $_" }) -join "`n"
        $replacement = "  encounterLocations:`n$locationLines`n"
    }
    else {
        $replacement = "  encounterLocations: []`n"
    }

    if ($Text -notmatch "(?m)^\s*encounterLocations:") {
        throw "encounterLocations field not found."
    }

    return [regex]::Replace(
        $Text,
        "(?ms)^  encounterLocations:.*?(?=^  learnableMoves:)",
        $replacement
    )
}

$encountersByName = @{}

Add-Encounter $encountersByName "Cave" @(
    "Sandshrew", "Diglett", "Geodude", "Onix", "Cubone", "Swinub", "Phanpy", "Omanyte", "Kabuto", "Rhyhorn", "Aerodactyl", "Mew"
)
Add-Encounter $encountersByName "Mountain" @(
    "Pikachu", "Mankey", "Machop", "Magnemite", "Voltorb", "Hitmonlee", "Hitmonchan", "Electabuzz", "Pichu", "Mareep", "Tyrogue", "Elekid", "Chinchou", "Corsola", "Smoochum", "Jynx", "Zapdos", "Raikou"
)
Add-Encounter $encountersByName "Overworld" @(
    "Mewtwo", "Larvitar", "Totodile", "Charmander", "Bulbasaur", "Squirtle", "Chikorita", "Cyndaquil"
)
Add-Encounter $encountersByName "Road01" @(
    "Caterpie", "Weedle", "Pidgey", "Rattata", "Spearow", "Farfetch'd", "Doduo", "Magikarp", "Dratini", "Hoothoot", "Ledyba", "Spinarak", "Pineco", "Sentret", "Murkrow", "Wooper", "Aipom", "Pinsir", "Yanma"
)
Add-Encounter $encountersByName "Road02" @(
    "Ekans", "Nidoran-F", "Nidoran-M", "Zubat", "Oddish", "Paras", "Venonat", "Poliwag", "Bellsprout", "Grimer", "Exeggcute", "Koffing", "Tangela", "Hoppip", "Sunkern", "Marill", "Qwilfish", "Heracross", "Scyther"
)
Add-Encounter $encountersByName "Road03" @(
    "Vulpix", "Meowth", "Psyduck", "Growlithe", "Ponyta", "Slowpoke", "Magmar", "Slugma", "Magby", "Houndour", "Lickitung", "Tauros", "Delibird", "Snorlax", "Moltres", "Entei"
)
Add-Encounter $encountersByName "Road04" @(
    "Gastly", "Mr. Mime", "Natu", "Misdreavus", "Girafarig", "Sneasel", "Teddiursa", "Stantler", "Abra", "Drowzee", "Kangaskhan", "Lugia"
)
Add-Encounter $encountersByName "WaterTown" @(
    "Tentacool", "Seel", "Shellder", "Krabby", "Horsea", "Goldeen", "Staryu", "Remoraid", "Mantine", "Articuno", "Suicune"
)
Add-Encounter $encountersByName "ChampionMeet" @(
    "Dunsparce", "Shuckle", "Sudowoodo", "Gligar", "Miltank", "Ho-Oh", "Skarmory"
)
Add-Encounter $encountersByName "GrassTown" @(
    "Cleffa", "Igglybuff", "Togepi", "Snubbull", "Chansey", "Celebi", "Eevee"
)
Add-Encounter $encountersByName "FireTown" @(
    "Smeargle", "Unown", "Wobbuffet", "Porygon"
)

$assetFiles = Get-ChildItem -LiteralPath $PokemonDataPath -Filter "*.asset" | Sort-Object Name
$assets = foreach ($file in $assetFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $guid = Get-AssetGuid $file.FullName
    $name = Get-PokemonName $text
    if ([string]::IsNullOrWhiteSpace($guid) -or [string]::IsNullOrWhiteSpace($name)) {
        continue
    }

    [pscustomobject]@{
        File = $file
        Text = $text
        Guid = $guid
        Name = $name
        Key = Normalize-PokemonName $name
    }
}

$assetByGuid = @{}
$assetByKey = @{}
foreach ($asset in $assets) {
    $assetByGuid[$asset.Guid] = $asset
    $assetByKey[$asset.Key] = $asset
}

$evolutionTargetGuids = [System.Collections.Generic.HashSet[string]]::new()
foreach ($asset in $assets) {
    foreach ($match in [regex]::Matches($asset.Text, "evolvesTo:\s*\{fileID:\s*11400000,\s*guid:\s*([0-9a-f]+),\s*type:\s*2\}")) {
        [void]$evolutionTargetGuids.Add($match.Groups[1].Value)
    }
}

$baseAssets = $assets | Where-Object { -not $evolutionTargetGuids.Contains($_.Guid) }
$baseKeys = [System.Collections.Generic.HashSet[string]]::new()
foreach ($asset in $baseAssets) {
    [void]$baseKeys.Add($asset.Key)
}

$updated = 0
$clearedEvolved = 0
$clearedUnlistedBase = 0
$listedButMissingAsset = [System.Collections.Generic.List[string]]::new()
$listedButEvolved = [System.Collections.Generic.List[string]]::new()

foreach ($entry in $encountersByName.GetEnumerator()) {
    if (-not $assetByKey.ContainsKey($entry.Key)) {
        $listedButMissingAsset.Add($entry.Key)
    }
    elseif (-not $baseKeys.Contains($entry.Key)) {
        $listedButEvolved.Add($assetByKey[$entry.Key].Name)
    }
}

foreach ($asset in $assets) {
    $locations = @()
    if ($baseKeys.Contains($asset.Key) -and $encountersByName.ContainsKey($asset.Key)) {
        $locations = @($encountersByName[$asset.Key])
    }

    $newText = Set-EncounterLocations $asset.Text $locations
    if ($newText -ne $asset.Text) {
        $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
        [System.IO.File]::WriteAllText($asset.File.FullName, $newText, $utf8NoBom)
        $updated++

        if (-not $baseKeys.Contains($asset.Key)) {
            $clearedEvolved++
        }
        elseif (-not $encountersByName.ContainsKey($asset.Key)) {
            $clearedUnlistedBase++
        }
    }
}

$unlistedBase = $baseAssets |
    Where-Object { -not $encountersByName.ContainsKey($_.Key) } |
    Sort-Object Name |
    Select-Object -ExpandProperty Name

Write-Host "Updated assets: $updated"
Write-Host "Cleared evolved forms: $clearedEvolved"
Write-Host "Cleared unlisted base forms: $clearedUnlistedBase"
Write-Host ""
Write-Host "Base forms not listed in document:"
if ($unlistedBase.Count -eq 0) {
    Write-Host "- None"
}
else {
    foreach ($name in $unlistedBase) {
        Write-Host "- $name"
    }
}

Write-Host ""
Write-Host "Names listed in document but ignored because they are evolved forms:"
if ($listedButEvolved.Count -eq 0) {
    Write-Host "- None"
}
else {
    $listedButEvolved | Sort-Object -Unique | ForEach-Object { Write-Host "- $_" }
}

Write-Host ""
Write-Host "Names listed in document but missing asset:"
if ($listedButMissingAsset.Count -eq 0) {
    Write-Host "- None"
}
else {
    $listedButMissingAsset | Sort-Object -Unique | ForEach-Object { Write-Host "- $_" }
}
