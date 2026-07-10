$ErrorActionPreference = "Stop"

$assetDir = Join-Path $PSScriptRoot "..\Assets\Game\Resources\PokemonData"
$assetDir = [System.IO.Path]::GetFullPath($assetDir)

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

# PokemonPersonality enum values:
# Playful=0, Brave=1, Timid=2, Proud=3, Gentle=4, Loyal=5, Curious=6, Lazy=7
$skipNames = @(
    "pikachu",
    "articuno",
    "zapdos",
    "moltres",
    "mewtwo",
    "mew"
)

$personalityOverrides = @{
    "bulbasaur"  = @{ Personality = 4; Chance = 100 }
    "ivysaur"    = @{ Personality = 4; Chance = 100 }
    "venusaur"   = @{ Personality = 4; Chance = 100 }
    "charmander" = @{ Personality = 1; Chance = 100 }
    "charmeleon" = @{ Personality = 3; Chance = 100 }
    "charizard"  = @{ Personality = 3; Chance = 100 }
    "squirtle"   = @{ Personality = 5; Chance = 100 }
    "wartortle"  = @{ Personality = 5; Chance = 100 }
    "blastoise"  = @{ Personality = 1; Chance = 100 }
    "caterpie"   = @{ Personality = 6; Chance = 100 }
    "metapod"    = @{ Personality = 7; Chance = 100 }
    "butterfree" = @{ Personality = 4; Chance = 100 }
    "weedle"     = @{ Personality = 6; Chance = 100 }
    "kakuna"     = @{ Personality = 7; Chance = 100 }
    "beedrill"   = @{ Personality = 1; Chance = 100 }
    "pidgey"     = @{ Personality = 2; Chance = 100 }
    "pidgeotto"  = @{ Personality = 1; Chance = 100 }
    "pidgeot"    = @{ Personality = 3; Chance = 100 }
    "rattata"    = @{ Personality = 0; Chance = 100 }
    "raticate"   = @{ Personality = 3; Chance = 100 }
    "spearow"    = @{ Personality = 3; Chance = 100 }
    "fearow"     = @{ Personality = 3; Chance = 100 }
    "ekans"      = @{ Personality = 2; Chance = 100 }
    "arbok"      = @{ Personality = 3; Chance = 100 }
    "raichu"     = @{ Personality = 0; Chance = 100 }
    "sandshrew"  = @{ Personality = 5; Chance = 100 }
    "sandslash"  = @{ Personality = 1; Chance = 100 }
    "nidoranf"   = @{ Personality = 1; Chance = 100 }
    "nidorina"   = @{ Personality = 1; Chance = 100 }
    "nidoqueen"  = @{ Personality = 3; Chance = 100 }
    "nidoranm"   = @{ Personality = 3; Chance = 100 }
    "nidorino"   = @{ Personality = 3; Chance = 100 }
    "nidoking"   = @{ Personality = 3; Chance = 100 }
    "clefairy"   = @{ Personality = 0; Chance = 100 }
    "clefable"   = @{ Personality = 4; Chance = 100 }
    "vulpix"     = @{ Personality = 6; Chance = 100 }
    "ninetales"  = @{ Personality = 3; Chance = 100 }
    "jigglypuff" = @{ Personality = 0; Chance = 100 }
    "wigglytuff" = @{ Personality = 0; Chance = 100 }
    "zubat"      = @{ Personality = 2; Chance = 100 }
    "golbat"     = @{ Personality = 3; Chance = 100 }
    "oddish"     = @{ Personality = 4; Chance = 100 }
    "gloom"      = @{ Personality = 7; Chance = 100 }
    "vileplume"  = @{ Personality = 4; Chance = 100 }
    "paras"      = @{ Personality = 2; Chance = 100 }
    "parasect"   = @{ Personality = 6; Chance = 100 }
    "venonat"    = @{ Personality = 6; Chance = 100 }
    "venomoth"   = @{ Personality = 6; Chance = 100 }
    "diglett"    = @{ Personality = 2; Chance = 100 }
    "dugtrio"    = @{ Personality = 5; Chance = 100 }
    "meowth"     = @{ Personality = 3; Chance = 100 }
    "persian"    = @{ Personality = 3; Chance = 100 }
    "psyduck"    = @{ Personality = 6; Chance = 100 }
    "golduck"    = @{ Personality = 4; Chance = 100 }
    "mankey"     = @{ Personality = 1; Chance = 100 }
    "primeape"   = @{ Personality = 1; Chance = 100 }
    "growlithe"  = @{ Personality = 5; Chance = 100 }
    "arcanine"   = @{ Personality = 5; Chance = 100 }
    "pol_key"    = @{ Personality = 4; Chance = 100 }
    "abra"       = @{ Personality = 6; Chance = 100 }
    "kadabra"    = @{ Personality = 6; Chance = 100 }
    "alakazam"   = @{ Personality = 6; Chance = 100 }
    "machop"     = @{ Personality = 1; Chance = 100 }
    "machoke"    = @{ Personality = 1; Chance = 100 }
    "machamp"    = @{ Personality = 1; Chance = 100 }
    "bellsprout" = @{ Personality = 4; Chance = 100 }
    "weepinbell" = @{ Personality = 7; Chance = 100 }
    "victreebel" = @{ Personality = 3; Chance = 100 }
    "tentacool"  = @{ Personality = 6; Chance = 100 }
    "tentacruel" = @{ Personality = 3; Chance = 100 }
    "geodude"    = @{ Personality = 3; Chance = 100 }
    "graveler"   = @{ Personality = 3; Chance = 100 }
    "golem"      = @{ Personality = 5; Chance = 100 }
    "ponyta"     = @{ Personality = 0; Chance = 100 }
    "rapidash"   = @{ Personality = 3; Chance = 100 }
    "slowpoke"   = @{ Personality = 7; Chance = 100 }
    "slowbro"    = @{ Personality = 7; Chance = 100 }
    "magnemite"  = @{ Personality = 5; Chance = 100 }
    "magneton"   = @{ Personality = 5; Chance = 100 }
    "farfetchd"  = @{ Personality = 3; Chance = 100 }
    "doduo"      = @{ Personality = 2; Chance = 100 }
    "dodrio"     = @{ Personality = 3; Chance = 100 }
    "seel"       = @{ Personality = 0; Chance = 100 }
    "dewgong"    = @{ Personality = 4; Chance = 100 }
    "grimer"     = @{ Personality = 7; Chance = 100 }
    "muk"        = @{ Personality = 7; Chance = 100 }
    "shellder"   = @{ Personality = 2; Chance = 100 }
    "cloyster"   = @{ Personality = 3; Chance = 100 }
    "gastly"     = @{ Personality = 0; Chance = 100 }
    "haunter"    = @{ Personality = 0; Chance = 100 }
    "gengar"     = @{ Personality = 0; Chance = 100 }
    "onix"       = @{ Personality = 3; Chance = 100 }
    "drowzee"    = @{ Personality = 6; Chance = 100 }
    "hypno"      = @{ Personality = 6; Chance = 100 }
    "krabby"     = @{ Personality = 3; Chance = 100 }
    "kingler"    = @{ Personality = 3; Chance = 100 }
    "voltorb"    = @{ Personality = 0; Chance = 100 }
    "electrode"  = @{ Personality = 0; Chance = 100 }
    "exeggcute"  = @{ Personality = 6; Chance = 100 }
    "exeggutor"  = @{ Personality = 7; Chance = 100 }
    "cubone"     = @{ Personality = 2; Chance = 100 }
    "marowak"    = @{ Personality = 5; Chance = 100 }
    "hitmonlee"  = @{ Personality = 1; Chance = 100 }
    "hitmonchan" = @{ Personality = 1; Chance = 100 }
    "lickitung"  = @{ Personality = 7; Chance = 100 }
    "koffing"    = @{ Personality = 0; Chance = 100 }
    "weezing"    = @{ Personality = 3; Chance = 100 }
    "rhyhorn"    = @{ Personality = 1; Chance = 100 }
    "rhydon"     = @{ Personality = 3; Chance = 100 }
    "chansey"    = @{ Personality = 4; Chance = 100 }
    "tangela"    = @{ Personality = 6; Chance = 100 }
    "kangaskhan" = @{ Personality = 5; Chance = 100 }
    "horsea"     = @{ Personality = 2; Chance = 100 }
    "seadra"     = @{ Personality = 3; Chance = 100 }
    "goldeen"    = @{ Personality = 3; Chance = 100 }
    "seaking"    = @{ Personality = 3; Chance = 100 }
    "staryu"     = @{ Personality = 6; Chance = 100 }
    "starmie"    = @{ Personality = 6; Chance = 100 }
    "mrmime"     = @{ Personality = 6; Chance = 100 }
    "scyther"    = @{ Personality = 3; Chance = 100 }
    "jynx"       = @{ Personality = 3; Chance = 100 }
    "electabuzz" = @{ Personality = 1; Chance = 100 }
    "magmar"     = @{ Personality = 1; Chance = 100 }
    "pinsir"     = @{ Personality = 1; Chance = 100 }
    "tauros"     = @{ Personality = 3; Chance = 100 }
    "magikarp"   = @{ Personality = 2; Chance = 100 }
    "gyarados"   = @{ Personality = 3; Chance = 100 }
    "lapras"     = @{ Personality = 4; Chance = 100 }
    "eevee"      = @{ Personality = 0; Chance = 100 }
    "vaporeon"   = @{ Personality = 4; Chance = 100 }
    "jolteon"    = @{ Personality = 0; Chance = 100 }
    "flareon"    = @{ Personality = 1; Chance = 100 }
    "porygon"    = @{ Personality = 6; Chance = 100 }
    "omanyte"    = @{ Personality = 6; Chance = 100 }
    "omastar"    = @{ Personality = 3; Chance = 100 }
    "kabuto"     = @{ Personality = 6; Chance = 100 }
    "kabutops"   = @{ Personality = 1; Chance = 100 }
    "aerodactyl" = @{ Personality = 3; Chance = 100 }
    "snorlax"    = @{ Personality = 7; Chance = 100 }
    "dratini"    = @{ Personality = 2; Chance = 100 }
    "dragonair"  = @{ Personality = 4; Chance = 100 }
    "dragonite"  = @{ Personality = 5; Chance = 100 }
}

# Typo-friendly aliases for grouped entries.
$personalityOverrides["poliwag"] = $personalityOverrides["pol_key"]
$personalityOverrides["poliwhirl"] = $personalityOverrides["pol_key"]
$personalityOverrides["poliwrath"] = @{ Personality = 1; Chance = 100 }
$personalityOverrides.Remove("pol_key")

$typePersonalityMap = @{
    1  = 5
    2  = 1
    3  = 4
    4  = 0
    5  = 4
    6  = 2
    7  = 1
    8  = 2
    9  = 5
    10 = 2
    11 = 6
    12 = 6
    13 = 3
    14 = 6
    15 = 3
    16 = 3
    17 = 5
    18 = 0
}

$updated = 0
$files = Get-ChildItem -Path $assetDir -Filter "*.asset" -File

foreach ($file in $files) {
    $text = Get-Content -Path $file.FullName -Raw
    $numMatch = [regex]::Match($text, "(?m)^  num: (\d+)$")
    if (-not $numMatch.Success) { continue }

    $num = [int]$numMatch.Groups[1].Value
    if ($num -lt 1 -or $num -gt 150) { continue }

    $assetName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name).ToLowerInvariant()
    if ($skipNames -contains $assetName) { continue }

    $personality = $null
    $chance = 80

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

    Set-Content -Path $file.FullName -Value $text -Encoding UTF8
    $updated++
}

Write-Host "Updated Gen 1 Pokemon personalities: $updated"
