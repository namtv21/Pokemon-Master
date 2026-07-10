$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$pokemonDir = Join-Path $root "Assets\Game\Resources\PokemonData"
$moveDir = Join-Path $root "Assets\Game\Resources\MoveData"

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

function Get-AssetGuid {
    param(
        [string]$Directory,
        [string]$AssetName
    )

    $metaPath = Join-Path $Directory "$AssetName.asset.meta"
    if (-not (Test-Path $metaPath)) {
        return $null
    }

    $guidLine = Select-String -Path $metaPath -Pattern "^guid: " | Select-Object -First 1
    if ($null -eq $guidLine) {
        return $null
    }

    return ($guidLine.Line -replace "^guid:\s*", "").Trim()
}

function Set-Learnset {
    param(
        [string]$Text,
        [array]$Moves
    )

    $lines = @("  learnableMoves:")
    foreach ($entry in $Moves) {
        $guid = Get-AssetGuid -Directory $moveDir -AssetName $entry.Move
        if ([string]::IsNullOrWhiteSpace($guid)) {
            throw "Missing move asset: $($entry.Move)"
        }

        $lines += "  - move: {fileID: 11400000, guid: $guid, type: 2}"
        $lines += "    level: $($entry.Level)"
    }

    $replacement = ($lines -join "`n") + "`n"
    return [regex]::Replace(
        $Text,
        "(?ms)^  learnableMoves:.*$",
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($m)
            $replacement
        }
    )
}

function Clear-EncounterLocations {
    param([string]$Text)

    return [regex]::Replace(
        $Text,
        "(?ms)^  encounterLocations:(?: \[\])?.*?(?=^  learnableMoves:)",
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($m)
            "  encounterLocations: []`n"
        }
    )
}

$specialLearnsets = @{
    "unown" = @(
        @{ Move = "hiddenpower"; Level = 1 },
        @{ Move = "confusion"; Level = 10 },
        @{ Move = "psybeam"; Level = 20 },
        @{ Move = "psychic"; Level = 35 },
        @{ Move = "futuresight"; Level = 45 }
    )
    "wobbuffet" = @(
        @{ Move = "splash"; Level = 1 },
        @{ Move = "protect"; Level = 1 },
        @{ Move = "confusion"; Level = 12 },
        @{ Move = "psybeam"; Level = 24 },
        @{ Move = "psychic"; Level = 36 }
    )
    "smeargle" = @(
        @{ Move = "tackle"; Level = 1 },
        @{ Move = "quickattack"; Level = 8 },
        @{ Move = "doubleslap"; Level = 18 },
        @{ Move = "swift"; Level = 30 },
        @{ Move = "struggle"; Level = 42 }
    )
}

$gen1EvolutionMap = @{
    "bulbasaur"  = @{ To = "ivysaur"; Level = 16 }
    "ivysaur"    = @{ To = "venusaur"; Level = 32 }
    "charmander" = @{ To = "charmeleon"; Level = 16 }
    "charmeleon" = @{ To = "charizard"; Level = 36 }
    "squirtle"   = @{ To = "wartortle"; Level = 16 }
    "wartortle"  = @{ To = "blastoise"; Level = 36 }
    "caterpie"   = @{ To = "metapod"; Level = 7 }
    "metapod"    = @{ To = "butterfree"; Level = 10 }
    "weedle"     = @{ To = "kakuna"; Level = 7 }
    "kakuna"     = @{ To = "beedrill"; Level = 10 }
    "pidgey"     = @{ To = "pidgeotto"; Level = 18 }
    "pidgeotto"  = @{ To = "pidgeot"; Level = 36 }
    "rattata"    = @{ To = "raticate"; Level = 20 }
    "spearow"    = @{ To = "fearow"; Level = 20 }
    "ekans"      = @{ To = "arbok"; Level = 22 }
    "pikachu"    = @{ To = "raichu"; Level = 22 }
    "sandshrew"  = @{ To = "sandslash"; Level = 22 }
    "nidoranf"   = @{ To = "nidorina"; Level = 16 }
    "nidorina"   = @{ To = "nidoqueen"; Level = 36 }
    "nidoranm"   = @{ To = "nidorino"; Level = 16 }
    "nidorino"   = @{ To = "nidoking"; Level = 36 }
    "clefairy"   = @{ To = "clefable"; Level = 36 }
    "vulpix"     = @{ To = "ninetales"; Level = 36 }
    "jigglypuff" = @{ To = "wigglytuff"; Level = 36 }
    "zubat"      = @{ To = "golbat"; Level = 22 }
    "golbat"     = @{ To = "crobat"; Level = 36 }
    "oddish"     = @{ To = "gloom"; Level = 21 }
    "gloom"      = @{ To = "vileplume"; Level = 36 }
    "paras"      = @{ To = "parasect"; Level = 24 }
    "venonat"    = @{ To = "venomoth"; Level = 31 }
    "diglett"    = @{ To = "dugtrio"; Level = 26 }
    "meowth"     = @{ To = "persian"; Level = 28 }
    "psyduck"    = @{ To = "golduck"; Level = 33 }
    "mankey"     = @{ To = "primeape"; Level = 28 }
    "growlithe"  = @{ To = "arcanine"; Level = 36 }
    "poliwag"    = @{ To = "poliwhirl"; Level = 25 }
    "poliwhirl"  = @{ To = "poliwrath"; Level = 36 }
    "abra"       = @{ To = "kadabra"; Level = 16 }
    "kadabra"    = @{ To = "alakazam"; Level = 36 }
    "machop"     = @{ To = "machoke"; Level = 28 }
    "machoke"    = @{ To = "machamp"; Level = 40 }
    "bellsprout" = @{ To = "weepinbell"; Level = 21 }
    "weepinbell" = @{ To = "victreebel"; Level = 36 }
    "tentacool"  = @{ To = "tentacruel"; Level = 30 }
    "geodude"    = @{ To = "graveler"; Level = 25 }
    "graveler"   = @{ To = "golem"; Level = 40 }
    "ponyta"     = @{ To = "rapidash"; Level = 40 }
    "slowpoke"   = @{ To = "slowbro"; Level = 37 }
    "magnemite"  = @{ To = "magneton"; Level = 30 }
    "doduo"      = @{ To = "dodrio"; Level = 31 }
    "seel"       = @{ To = "dewgong"; Level = 34 }
    "grimer"     = @{ To = "muk"; Level = 38 }
    "shellder"   = @{ To = "cloyster"; Level = 36 }
    "gastly"     = @{ To = "haunter"; Level = 25 }
    "haunter"    = @{ To = "gengar"; Level = 40 }
    "onix"       = @{ To = "steelix"; Level = 40 }
    "drowzee"    = @{ To = "hypno"; Level = 26 }
    "krabby"     = @{ To = "kingler"; Level = 28 }
    "voltorb"    = @{ To = "electrode"; Level = 30 }
    "exeggcute"  = @{ To = "exeggutor"; Level = 36 }
    "cubone"     = @{ To = "marowak"; Level = 28 }
    "koffing"    = @{ To = "weezing"; Level = 35 }
    "rhyhorn"    = @{ To = "rhydon"; Level = 42 }
    "chansey"    = @{ To = "blissey"; Level = 40 }
    "horsea"     = @{ To = "seadra"; Level = 32 }
    "seadra"     = @{ To = "kingdra"; Level = 40 }
    "goldeen"    = @{ To = "seaking"; Level = 33 }
    "staryu"     = @{ To = "starmie"; Level = 36 }
    "scyther"    = @{ To = "scizor"; Level = 40 }
    "magikarp"   = @{ To = "gyarados"; Level = 20 }
    "eevee"      = @{ To = "vaporeon"; Level = 20 }
    "porygon"    = @{ To = "porygon2"; Level = 30 }
    "omanyte"    = @{ To = "omastar"; Level = 40 }
    "kabuto"     = @{ To = "kabutops"; Level = 40 }
    "dratini"    = @{ To = "dragonair"; Level = 30 }
    "dragonair"  = @{ To = "dragonite"; Level = 55 }
}

$evolvedFormsToKeepWildFree = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $gen1EvolutionMap.GetEnumerator()) {
    [void]$evolvedFormsToKeepWildFree.Add($entry.Value.To)
}

$specialUpdated = 0
foreach ($name in $specialLearnsets.Keys) {
    $path = Join-Path $pokemonDir "$name.asset"
    if (-not (Test-Path $path)) {
        Write-Warning "Missing Pokemon asset for special learnset: $name"
        continue
    }

    $text = Get-Content -Path $path -Raw
    $text = Set-Learnset -Text $text -Moves $specialLearnsets[$name]
    Set-Content -Path $path -Value $text -Encoding UTF8
    $specialUpdated++
}

$evolutionUpdated = 0
$wildLocationsCleared = 0
$files = Get-ChildItem -Path $pokemonDir -Filter "*.asset" -File

foreach ($file in $files) {
    $text = Get-Content -Path $file.FullName -Raw
    $numMatch = [regex]::Match($text, "(?m)^  num: (\d+)$")
    if (-not $numMatch.Success) { continue }

    $num = [int]$numMatch.Groups[1].Value
    if ($num -lt 1 -or $num -gt 150) { continue }

    $assetName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name).ToLowerInvariant()
    if ($gen1EvolutionMap.ContainsKey($assetName)) {
        $target = $gen1EvolutionMap[$assetName].To
        $targetGuid = Get-AssetGuid -Directory $pokemonDir -AssetName $target

        if ([string]::IsNullOrWhiteSpace($targetGuid)) {
            Write-Warning "Skip $assetName evolution: missing target asset $target"
        }
        else {
            $text = Set-ScalarField -Text $text -Field "evolvable" -Value "1"
            $text = Set-ScalarField -Text $text -Field "evolutionLevel" -Value ([string]$gen1EvolutionMap[$assetName].Level)
            $text = Set-ScalarField -Text $text -Field "evolvesTo" -Value "{fileID: 11400000, guid: $targetGuid, type: 2}"
            $evolutionUpdated++
        }
    }
    else {
        $text = Set-ScalarField -Text $text -Field "evolvable" -Value "0"
        $text = Set-ScalarField -Text $text -Field "evolutionLevel" -Value "0"
        $text = Set-ScalarField -Text $text -Field "evolvesTo" -Value "{fileID: 0}"
    }

    if ($evolvedFormsToKeepWildFree.Contains($assetName)) {
        $before = $text
        $text = Clear-EncounterLocations -Text $text
        if ($text -ne $before) {
            $wildLocationsCleared++
        }
    }

    Set-Content -Path $file.FullName -Value $text -Encoding UTF8
}

Write-Host "Special learnsets updated: $specialUpdated"
Write-Host "Gen 1 evolutions updated: $evolutionUpdated"
Write-Host "Gen 1 evolved-form wild locations cleared: $wildLocationsCleared"
