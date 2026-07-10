$ErrorActionPreference = "Stop"

$assetDir = Join-Path $PSScriptRoot "..\Assets\Game\Resources\PokemonData"
$assetDir = [System.IO.Path]::GetFullPath($assetDir)

if (-not (Test-Path $assetDir)) {
    throw "PokemonData folder not found: $assetDir"
}

function Set-ScalarField {
    param(
        [string]$Text,
        [string]$Field,
        [string]$Value
    )

    $pattern = "(?m)^  $([regex]::Escape($Field)):.*$"
    $replacement = "  ${Field}: $Value"
    return [regex]::Replace($Text, $pattern, $replacement)
}

function Set-EncounterLocations {
    param(
        [string]$Text,
        [string[]]$Locations
    )

    $replacement = if ($Locations -and $Locations.Count -gt 0) {
        "  encounterLocations:`n" + (($Locations | ForEach-Object { "  - $_" }) -join "`n") + "`n"
    }
    else {
        "  encounterLocations: []`n"
    }

    return [regex]::Replace(
        $Text,
        "(?ms)^  encounterLocations:(?: \[\])?.*?(?=^  learnableMoves:)",
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($m)
            $replacement
        }
    )
}

function Get-AssetGuid {
    param([string]$AssetName)

    $metaPath = Join-Path $assetDir "$AssetName.asset.meta"
    if (-not (Test-Path $metaPath)) {
        throw "Missing meta file for $AssetName"
    }

    $guidLine = Select-String -Path $metaPath -Pattern "^guid: " | Select-Object -First 1
    if ($null -eq $guidLine) {
        throw "Missing guid in $metaPath"
    }

    return ($guidLine.Line -replace "^guid:\s*", "").Trim()
}

$evolutionMap = @{
    "chikorita" = @{ To = "bayleef"; Level = 16 }
    "bayleef"   = @{ To = "meganium"; Level = 32 }
    "cyndaquil" = @{ To = "quilava"; Level = 14 }
    "quilava"   = @{ To = "typhlosion"; Level = 36 }
    "totodile"  = @{ To = "croconaw"; Level = 18 }
    "croconaw"  = @{ To = "feraligatr"; Level = 30 }
    "sentret"   = @{ To = "furret"; Level = 15 }
    "hoothoot"  = @{ To = "noctowl"; Level = 20 }
    "ledyba"    = @{ To = "ledian"; Level = 18 }
    "spinarak"  = @{ To = "ariados"; Level = 22 }
    "chinchou"  = @{ To = "lanturn"; Level = 27 }
    "pichu"     = @{ To = "pikachu"; Level = 18 }
    "cleffa"    = @{ To = "clefairy"; Level = 16 }
    "igglybuff" = @{ To = "jigglypuff"; Level = 16 }
    "togepi"    = @{ To = "togetic"; Level = 20 }
    "natu"      = @{ To = "xatu"; Level = 25 }
    "mareep"    = @{ To = "flaaffy"; Level = 15 }
    "flaaffy"   = @{ To = "ampharos"; Level = 30 }
    "marill"    = @{ To = "azumarill"; Level = 18 }
    "hoppip"    = @{ To = "skiploom"; Level = 18 }
    "skiploom"  = @{ To = "jumpluff"; Level = 27 }
    "sunkern"   = @{ To = "sunflora"; Level = 18 }
    "wooper"    = @{ To = "quagsire"; Level = 20 }
    "pineco"    = @{ To = "forretress"; Level = 31 }
    "snubbull"  = @{ To = "granbull"; Level = 23 }
    "teddiursa" = @{ To = "ursaring"; Level = 30 }
    "slugma"    = @{ To = "magcargo"; Level = 38 }
    "swinub"    = @{ To = "piloswine"; Level = 33 }
    "remoraid"  = @{ To = "octillery"; Level = 25 }
    "houndour"  = @{ To = "houndoom"; Level = 24 }
    "phanpy"    = @{ To = "donphan"; Level = 25 }
    "tyrogue"   = @{ To = "hitmontop"; Level = 20 }
    "smoochum"  = @{ To = "jynx"; Level = 30 }
    "elekid"    = @{ To = "electabuzz"; Level = 30 }
    "magby"     = @{ To = "magmar"; Level = 30 }
    "larvitar"  = @{ To = "pupitar"; Level = 30 }
    "pupitar"   = @{ To = "tyranitar"; Level = 55 }
}

$encounterMap = @{
    "chikorita" = @("Road02")
    "cyndaquil" = @("Road03")
    "totodile"  = @("WaterTown")
    "sentret"   = @("Road03")
    "hoothoot"  = @("Road01")
    "ledyba"    = @("Road01")
    "spinarak"  = @("Road01")
    "chinchou"  = @("WaterTown")
    "pichu"     = @("Mountain")
    "cleffa"    = @("Road04")
    "igglybuff" = @("Road04")
    "togepi"    = @("Road04")
    "natu"      = @("Road04")
    "mareep"    = @("Mountain")
    "marill"    = @("WaterTown")
    "hoppip"    = @("Road02")
    "aipom"     = @("Road03")
    "sunkern"   = @("Road02")
    "yanma"     = @("Road01")
    "wooper"    = @("WaterTown")
    "murkrow"   = @("Road04")
    "misdreavus" = @("Road04")
    "unown"     = @("Road04")
    "wobbuffet" = @("Road04")
    "girafarig" = @("Road04")
    "pineco"    = @("Road01")
    "dunsparce" = @("Cave")
    "gligar"    = @("Cave")
    "snubbull"  = @("Road04")
    "qwilfish"  = @("WaterTown")
    "shuckle"   = @("Cave")
    "heracross" = @("Road01")
    "sneasel"   = @("Road04")
    "teddiursa" = @("Road03")
    "slugma"    = @("Road03")
    "swinub"    = @("Cave")
    "corsola"   = @("WaterTown")
    "remoraid"  = @("WaterTown")
    "delibird"  = @("Road01")
    "mantine"   = @("WaterTown")
    "skarmory"  = @("Road01")
    "houndour"  = @("Road04")
    "phanpy"    = @("Cave")
    "stantler"  = @("Road03")
    "smeargle"  = @("Road03")
    "tyrogue"   = @("Mountain")
    "smoochum"  = @("Road04")
    "elekid"    = @("Mountain")
    "magby"     = @("Road03")
    "miltank"   = @("Road03")
    "larvitar"  = @("Cave")
}

# enum: Playful=0, Brave=1, Timid=2, Proud=3, Gentle=4, Loyal=5, Curious=6, Lazy=7
$personalityOverrides = @{
    "chikorita" = @{ Personality = 4; Chance = 100 }
    "bayleef"   = @{ Personality = 4; Chance = 100 }
    "meganium"  = @{ Personality = 4; Chance = 100 }
    "cyndaquil" = @{ Personality = 2; Chance = 100 }
    "quilava"   = @{ Personality = 1; Chance = 90 }
    "typhlosion" = @{ Personality = 1; Chance = 100 }
    "totodile"  = @{ Personality = 0; Chance = 100 }
    "croconaw"  = @{ Personality = 0; Chance = 100 }
    "feraligatr" = @{ Personality = 1; Chance = 90 }
    "pichu"     = @{ Personality = 0; Chance = 100 }
    "cleffa"    = @{ Personality = 0; Chance = 100 }
    "igglybuff" = @{ Personality = 0; Chance = 100 }
    "togepi"    = @{ Personality = 6; Chance = 100 }
    "togetic"   = @{ Personality = 4; Chance = 100 }
    "mareep"    = @{ Personality = 4; Chance = 100 }
    "ampharos"  = @{ Personality = 5; Chance = 100 }
    "marill"    = @{ Personality = 0; Chance = 100 }
    "azumarill" = @{ Personality = 0; Chance = 100 }
    "yanma"     = @{ Personality = 6; Chance = 100 }
    "wooper"    = @{ Personality = 0; Chance = 100 }
    "quagsire"  = @{ Personality = 7; Chance = 100 }
    "umbreon"   = @{ Personality = 5; Chance = 100 }
    "espeon"    = @{ Personality = 6; Chance = 100 }
    "murkrow"   = @{ Personality = 3; Chance = 100 }
    "misdreavus" = @{ Personality = 6; Chance = 100 }
    "unown"     = @{ Personality = 6; Chance = 100 }
    "wobbuffet" = @{ Personality = 5; Chance = 100 }
    "dunsparce" = @{ Personality = 7; Chance = 100 }
    "snubbull"  = @{ Personality = 3; Chance = 100 }
    "granbull"  = @{ Personality = 3; Chance = 100 }
    "shuckle"   = @{ Personality = 7; Chance = 100 }
    "heracross" = @{ Personality = 1; Chance = 100 }
    "sneasel"   = @{ Personality = 3; Chance = 100 }
    "teddiursa" = @{ Personality = 0; Chance = 100 }
    "ursaring"  = @{ Personality = 1; Chance = 100 }
    "slugma"    = @{ Personality = 7; Chance = 100 }
    "magcargo"  = @{ Personality = 7; Chance = 100 }
    "corsola"   = @{ Personality = 0; Chance = 100 }
    "delibird"  = @{ Personality = 0; Chance = 100 }
    "skarmory"  = @{ Personality = 3; Chance = 100 }
    "houndour"  = @{ Personality = 3; Chance = 100 }
    "houndoom"  = @{ Personality = 3; Chance = 100 }
    "phanpy"    = @{ Personality = 0; Chance = 100 }
    "donphan"   = @{ Personality = 1; Chance = 100 }
    "stantler"  = @{ Personality = 4; Chance = 100 }
    "smeargle"  = @{ Personality = 6; Chance = 100 }
    "tyrogue"   = @{ Personality = 1; Chance = 100 }
    "hitmontop" = @{ Personality = 1; Chance = 100 }
    "smoochum"  = @{ Personality = 3; Chance = 100 }
    "elekid"    = @{ Personality = 0; Chance = 100 }
    "electabuzz" = @{ Personality = 1; Chance = 100 }
    "magby"     = @{ Personality = 0; Chance = 100 }
    "magmar"    = @{ Personality = 1; Chance = 100 }
    "miltank"   = @{ Personality = 4; Chance = 100 }
    "blissey"   = @{ Personality = 4; Chance = 100 }
    "larvitar"  = @{ Personality = 1; Chance = 100 }
    "pupitar"   = @{ Personality = 3; Chance = 100 }
    "tyranitar" = @{ Personality = 3; Chance = 100 }
}

$legendaryNames = @("raikou", "entei", "suicune", "lugia", "hooh", "celebi")

$typePersonalityMap = @{
    1  = 5 # Normal -> Loyal
    2  = 1 # Fire -> Brave
    3  = 4 # Water -> Gentle
    4  = 0 # Electric -> Playful
    5  = 4 # Grass -> Gentle
    6  = 2 # Ice -> Timid
    7  = 1 # Fighting -> Brave
    8  = 2 # Poison -> Timid
    9  = 5 # Ground -> Loyal
    10 = 2 # Flying -> Timid
    11 = 6 # Psychic -> Curious
    12 = 6 # Bug -> Curious
    13 = 3 # Rock -> Proud
    14 = 6 # Ghost -> Curious
    15 = 3 # Dragon -> Proud
    16 = 3 # Dark -> Proud
    17 = 5 # Steel -> Loyal
    18 = 0 # Fairy -> Playful
}

$files = Get-ChildItem -Path $assetDir -Filter *.asset -File
$updated = 0

foreach ($file in $files) {
    $text = Get-Content -Path $file.FullName -Raw
    $numMatch = [regex]::Match($text, "(?m)^  num: (\d+)$")
    if (-not $numMatch.Success) { continue }

    $num = [int]$numMatch.Groups[1].Value
    if ($num -lt 152 -or $num -gt 251) { continue }

    $assetName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name).ToLowerInvariant()

    if ($evolutionMap.ContainsKey($assetName)) {
        $targetName = $evolutionMap[$assetName].To
        $targetGuid = Get-AssetGuid -AssetName $targetName
        $text = Set-ScalarField -Text $text -Field "evolvable" -Value "1"
        $text = Set-ScalarField -Text $text -Field "evolutionLevel" -Value ([string]$evolutionMap[$assetName].Level)
        $text = Set-ScalarField -Text $text -Field "evolvesTo" -Value "{fileID: 11400000, guid: $targetGuid, type: 2}"
    }
    else {
        $text = Set-ScalarField -Text $text -Field "evolvable" -Value "0"
        $text = Set-ScalarField -Text $text -Field "evolutionLevel" -Value "0"
        $text = Set-ScalarField -Text $text -Field "evolvesTo" -Value "{fileID: 0}"
    }

    $locations = @()
    if ($encounterMap.ContainsKey($assetName)) {
        $locations = $encounterMap[$assetName]
    }
    $text = Set-EncounterLocations -Text $text -Locations $locations

    if ($legendaryNames -notcontains $assetName) {
        $personality = $null
        $chance = 75

        if ($personalityOverrides.ContainsKey($assetName)) {
            $personality = $personalityOverrides[$assetName].Personality
            $chance = $personalityOverrides[$assetName].Chance
        }
        else {
            $type1Match = [regex]::Match($text, "(?m)^  type1: (\d+)$")
            $type2Match = [regex]::Match($text, "(?m)^  type2: (\d+)$")
            $type1 = if ($type1Match.Success) { [int]$type1Match.Groups[1].Value } else { 1 }
            $type2 = if ($type2Match.Success) { [int]$type2Match.Groups[1].Value } else { 0 }

            if ($typePersonalityMap.ContainsKey($type1)) {
                $personality = $typePersonalityMap[$type1]
            }
            elseif ($typePersonalityMap.ContainsKey($type2)) {
                $personality = $typePersonalityMap[$type2]
            }
            else {
                $personality = 0
            }
        }

        $text = Set-ScalarField -Text $text -Field "biasPersonality" -Value "1"
        $text = Set-ScalarField -Text $text -Field "preferredPersonality" -Value ([string]$personality)
        $text = Set-ScalarField -Text $text -Field "preferredPersonalityChance" -Value ([string]$chance)
    }

    Set-Content -Path $file.FullName -Value $text -Encoding UTF8
    $updated++
}

Write-Host "Updated Gen 2 Pokemon assets: $updated"
