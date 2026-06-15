@echo off
REM =====================================================
REM  HUONG DAN:
REM  1. Vao Cloudflare Dashboard -> Workers & Pages -> KV
REM  2. Click vao "GAME_DATA" namespace
REM  3. Copy ID tu URL (dang: /kv/namespaces/abc123...)
REM  4. Dan vao dong NAMESPACE_ID ben duoi
REM  5. Chay file nay (double-click hoac cmd)
REM  NOTE: Can chay "wrangler login" truoc neu chua dang nhap
REM =====================================================

set NAMESPACE_ID=PASTE_YOUR_NAMESPACE_ID_HERE

if "%NAMESPACE_ID%"=="PASTE_YOUR_NAMESPACE_ID_HERE" (
    echo [LOI] Ban chua dien NAMESPACE_ID!
    echo Mo file nay bang Notepad va sua dong "set NAMESPACE_ID=..."
    pause
    exit /b 1
)

echo Uploading game data to Cloudflare KV (namespace: %NAMESPACE_ID%)...
echo.

wrangler kv key put --namespace-id=%NAMESPACE_ID% "story_lore" --path=story_lore.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "all_items" --path=all_items.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "all_pokemon" --path=all_pokemon.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_abra" --path=pokemon_abra.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_aerodactyl" --path=pokemon_aerodactyl.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_alakazam" --path=pokemon_alakazam.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_arbok" --path=pokemon_arbok.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_arcanine" --path=pokemon_arcanine.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_articuno" --path=pokemon_articuno.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_beedrill" --path=pokemon_beedrill.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_bellsprout" --path=pokemon_bellsprout.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_blastoise" --path=pokemon_blastoise.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_bulbasaur" --path=pokemon_bulbasaur.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_butterfree" --path=pokemon_butterfree.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_caterpie" --path=pokemon_caterpie.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_chansey" --path=pokemon_chansey.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_charizard" --path=pokemon_charizard.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_charmander" --path=pokemon_charmander.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_charmeleon" --path=pokemon_charmeleon.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_clefable" --path=pokemon_clefable.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_clefairy" --path=pokemon_clefairy.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_cloyster" --path=pokemon_cloyster.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_cubone" --path=pokemon_cubone.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_dewgong" --path=pokemon_dewgong.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_diglett" --path=pokemon_diglett.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_dodrio" --path=pokemon_dodrio.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_doduo" --path=pokemon_doduo.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_dragonair" --path=pokemon_dragonair.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_dragonite" --path=pokemon_dragonite.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_dratini" --path=pokemon_dratini.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_drowzee" --path=pokemon_drowzee.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_dugtrio" --path=pokemon_dugtrio.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_eevee" --path=pokemon_eevee.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_ekans" --path=pokemon_ekans.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_electabuzz" --path=pokemon_electabuzz.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_electrode" --path=pokemon_electrode.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_exeggcute" --path=pokemon_exeggcute.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_exeggutor" --path=pokemon_exeggutor.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_farfetch’d" --path=pokemon_farfetch’d.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_fearow" --path=pokemon_fearow.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_flareon" --path=pokemon_flareon.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_gastly" --path=pokemon_gastly.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_gengar" --path=pokemon_gengar.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_geodude" --path=pokemon_geodude.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_gloom" --path=pokemon_gloom.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_golbat" --path=pokemon_golbat.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_goldeen" --path=pokemon_goldeen.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_golduck" --path=pokemon_golduck.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_golem" --path=pokemon_golem.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_graveler" --path=pokemon_graveler.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_grimer" --path=pokemon_grimer.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_growlithe" --path=pokemon_growlithe.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_gyarados" --path=pokemon_gyarados.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_haunter" --path=pokemon_haunter.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_hitmonchan" --path=pokemon_hitmonchan.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_hitmonlee" --path=pokemon_hitmonlee.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_horsea" --path=pokemon_horsea.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_hypno" --path=pokemon_hypno.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_ivysaur" --path=pokemon_ivysaur.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_jigglypuff" --path=pokemon_jigglypuff.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_jolteon" --path=pokemon_jolteon.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_jynx" --path=pokemon_jynx.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_kabuto" --path=pokemon_kabuto.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_kabutops" --path=pokemon_kabutops.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_kadabra" --path=pokemon_kadabra.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_kakuna" --path=pokemon_kakuna.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_kangaskhan" --path=pokemon_kangaskhan.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_kingler" --path=pokemon_kingler.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_koffing" --path=pokemon_koffing.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_krabby" --path=pokemon_krabby.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_lapras" --path=pokemon_lapras.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_lickitung" --path=pokemon_lickitung.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_machamp" --path=pokemon_machamp.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_machoke" --path=pokemon_machoke.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_machop" --path=pokemon_machop.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_magikarp" --path=pokemon_magikarp.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_magmar" --path=pokemon_magmar.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_magnemite" --path=pokemon_magnemite.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_magneton" --path=pokemon_magneton.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_mankey" --path=pokemon_mankey.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_marowak" --path=pokemon_marowak.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_meowth" --path=pokemon_meowth.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_metapod" --path=pokemon_metapod.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_mew" --path=pokemon_mew.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_mewtwo" --path=pokemon_mewtwo.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_moltres" --path=pokemon_moltres.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_mr._mime" --path=pokemon_mr._mime.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_muk" --path=pokemon_muk.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_nidoking" --path=pokemon_nidoking.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_nidoqueen" --path=pokemon_nidoqueen.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_nidoran-f" --path=pokemon_nidoran-f.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_nidoran-m" --path=pokemon_nidoran-m.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_nidorina" --path=pokemon_nidorina.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_nidorino" --path=pokemon_nidorino.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_ninetales" --path=pokemon_ninetales.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_oddish" --path=pokemon_oddish.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_omanyte" --path=pokemon_omanyte.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_omastar" --path=pokemon_omastar.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_onix" --path=pokemon_onix.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_paras" --path=pokemon_paras.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_parasect" --path=pokemon_parasect.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_persian" --path=pokemon_persian.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_pidgeot" --path=pokemon_pidgeot.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_pidgeotto" --path=pokemon_pidgeotto.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_pidgey" --path=pokemon_pidgey.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_pikachu" --path=pokemon_pikachu.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_pinsir" --path=pokemon_pinsir.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_poliwag" --path=pokemon_poliwag.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_poliwhirl" --path=pokemon_poliwhirl.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_poliwrath" --path=pokemon_poliwrath.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_ponyta" --path=pokemon_ponyta.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_porygon" --path=pokemon_porygon.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_primeape" --path=pokemon_primeape.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_psyduck" --path=pokemon_psyduck.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_raichu" --path=pokemon_raichu.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_rapidash" --path=pokemon_rapidash.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_raticate" --path=pokemon_raticate.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_rattata" --path=pokemon_rattata.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_rhydon" --path=pokemon_rhydon.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_rhyhorn" --path=pokemon_rhyhorn.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_sandshrew" --path=pokemon_sandshrew.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_sandslash" --path=pokemon_sandslash.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_scyther" --path=pokemon_scyther.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_seadra" --path=pokemon_seadra.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_seaking" --path=pokemon_seaking.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_seel" --path=pokemon_seel.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_shellder" --path=pokemon_shellder.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_slowbro" --path=pokemon_slowbro.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_slowpoke" --path=pokemon_slowpoke.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_snorlax" --path=pokemon_snorlax.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_spearow" --path=pokemon_spearow.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_squirtle" --path=pokemon_squirtle.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_starmie" --path=pokemon_starmie.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_staryu" --path=pokemon_staryu.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_tangela" --path=pokemon_tangela.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_tauros" --path=pokemon_tauros.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_tentacool" --path=pokemon_tentacool.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_tentacruel" --path=pokemon_tentacruel.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_vaporeon" --path=pokemon_vaporeon.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_venomoth" --path=pokemon_venomoth.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_venonat" --path=pokemon_venonat.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_venusaur" --path=pokemon_venusaur.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_victreebel" --path=pokemon_victreebel.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_vileplume" --path=pokemon_vileplume.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_voltorb" --path=pokemon_voltorb.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_vulpix" --path=pokemon_vulpix.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_wartortle" --path=pokemon_wartortle.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_weedle" --path=pokemon_weedle.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_weepinbell" --path=pokemon_weepinbell.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_weezing" --path=pokemon_weezing.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_wigglytuff" --path=pokemon_wigglytuff.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_zapdos" --path=pokemon_zapdos.json
wrangler kv key put --namespace-id=%NAMESPACE_ID% "pokemon_zubat" --path=pokemon_zubat.json

echo.
echo Done! Kiem tra KV tren Cloudflare Dashboard.
pause
