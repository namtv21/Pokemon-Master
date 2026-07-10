$ErrorActionPreference = "Stop"

$pokemonDir = Join-Path $PSScriptRoot "..\Assets\Game\Resources\PokemonData"
$pokemonDir = [System.IO.Path]::GetFullPath($pokemonDir)

function Get-AssetGuid {
    param([string]$AssetName)

    $metaPath = Join-Path $pokemonDir "$AssetName.asset.meta"
    if (-not (Test-Path $metaPath)) {
        throw "Missing meta file for $AssetName"
    }

    $guidLine = Select-String -Path $metaPath -Pattern "^guid: " | Select-Object -First 1
    if ($null -eq $guidLine) {
        throw "Missing guid in $metaPath"
    }

    return ($guidLine.Line -replace "^guid:\s*", "").Trim()
}

function Set-ScalarField {
    param(
        [string]$Text,
        [string]$Field,
        [string]$Value
    )

    return [regex]::Replace($Text, "(?m)^  $([regex]::Escape($Field)):.*$", "  ${Field}: $Value")
}

function Set-EvolutionOptions {
    param(
        [string]$Text,
        [array]$Options
    )

    $lines = @("  evolutionOptions:")
    foreach ($option in $Options) {
        $guid = Get-AssetGuid -AssetName $option.To
        $label = if ([string]::IsNullOrWhiteSpace($option.Label)) { $option.To } else { $option.Label }
        $lines += "  - evolvesTo: {fileID: 11400000, guid: $guid, type: 2}"
        $lines += "    evolutionLevel: $($option.Level)"
        $lines += "    label: $label"
    }
    $block = ($lines -join "`n")

    if ($Text -match "(?m)^  evolutionOptions:") {
        return [regex]::Replace(
            $Text,
            "(?ms)^  evolutionOptions:.*?(?=^  biasPersonality:)",
            [System.Text.RegularExpressions.MatchEvaluator]{ param($m) "$block`n" }
        )
    }

    return [regex]::Replace(
        $Text,
        "(?m)^  evolvesTo:.*$",
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($m)
            "$($m.Value)`n$block"
        }
    )
}

$branchMap = @{
    "slowpoke" = @(
        @{ To = "slowbro"; Level = 37; Label = "Slowbro" },
        @{ To = "slowking"; Level = 37; Label = "Slowking" }
    )
    "eevee" = @(
        @{ To = "vaporeon"; Level = 20; Label = "Vaporeon" },
        @{ To = "jolteon"; Level = 20; Label = "Jolteon" },
        @{ To = "flareon"; Level = 20; Label = "Flareon" },
        @{ To = "espeon"; Level = 30; Label = "Espeon" },
        @{ To = "umbreon"; Level = 30; Label = "Umbreon" }
    )
    "poliwhirl" = @(
        @{ To = "poliwrath"; Level = 36; Label = "Poliwrath" },
        @{ To = "politoed"; Level = 36; Label = "Politoed" }
    )
    "gloom" = @(
        @{ To = "vileplume"; Level = 36; Label = "Vileplume" },
        @{ To = "bellossom"; Level = 36; Label = "Bellossom" }
    )
    "tyrogue" = @(
        @{ To = "hitmonlee"; Level = 20; Label = "Hitmonlee" },
        @{ To = "hitmonchan"; Level = 20; Label = "Hitmonchan" },
        @{ To = "hitmontop"; Level = 20; Label = "Hitmontop" }
    )
}

$updated = 0
foreach ($name in $branchMap.Keys) {
    $path = Join-Path $pokemonDir "$name.asset"
    if (-not (Test-Path $path)) {
        Write-Warning "Missing Pokemon asset: $name"
        continue
    }

    $options = $branchMap[$name]
    $defaultGuid = Get-AssetGuid -AssetName $options[0].To
    $text = Get-Content -Path $path -Raw
    $text = Set-ScalarField -Text $text -Field "evolvable" -Value "1"
    $text = Set-ScalarField -Text $text -Field "evolutionLevel" -Value ([string]$options[0].Level)
    $text = Set-ScalarField -Text $text -Field "evolvesTo" -Value "{fileID: 11400000, guid: $defaultGuid, type: 2}"
    $text = Set-EvolutionOptions -Text $text -Options $options
    Set-Content -Path $path -Value $text -Encoding UTF8
    $updated++
}

Write-Host "Updated branch evolution Pokemon assets: $updated"
