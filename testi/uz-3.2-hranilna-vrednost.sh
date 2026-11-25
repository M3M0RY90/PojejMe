#!/usr/bin/env bash
# Avtomatsko preverjanje pogojev zadovoljstva UZ-3.2 Hranilna vrednost recepta (#25)
#
# Uporaba:
#   1. docker compose up -d          (baza)
#   2. cd PojejMe.Api && dotnet run  (API, privzeto http://localhost:5165)
#   3. v drugem terminalu: bash testi/uz-3.2-hranilna-vrednost.sh [URL]
#
# Testna živila imajo znane vrednosti na 100 g, zato je pričakovani rezultat izračunan vnaprej:
#   moka:  364 kcal, 10 g beljakovin, 76 g OH, 1 g maščob
#   mleko:  64 kcal, 3.3 g beljakovin, 4.8 g OH, 3.6 g maščob
#   Recept A = 250 g moke + 0.5 l mleka, 4 porcije
#     skupaj:     1230 kcal, 41.5 g B, 214 g OH, 20.5 g M
#     na porcijo: 307.5 kcal, 10.4 g B, 53.5 g OH, 5.1 g M
#   Recept B = 1 kg moke, 2 porciji
#     skupaj:     3640 kcal, 100 g B, 760 g OH, 10 g M
#     na porcijo: 1820 kcal, 50 g B, 380 g OH, 5 g M

URL="${1:-http://localhost:5165}"
ADMIN_KLJUC="lokalni-admin-kljuc"
TMP="$(mktemp -d)"
USPELO=0
PADLO=0
CAS="$(date +%s)"

klic() {
  local args=(-s -o "$TMP/telo" -w "%{http_code}" -X "$1" "$URL$2")
  [ -n "$3" ] && args+=(-H "Content-Type: application/json" --data "$3")
  [ -n "$4" ] && args+=(-H "Authorization: Bearer $4")
  KODA=$(curl "${args[@]}")
  TELO=$(cat "$TMP/telo" 2>/dev/null)
}

preveri() {
  if [ "$KODA" = "$2" ]; then
    echo "  ✔ $1 (HTTP $KODA)"; USPELO=$((USPELO + 1))
  else
    echo "  ✘ $1 (pričakovano $2, dobljeno $KODA)"
    echo "    odgovor: $(echo "$TELO" | head -c 300)"
    PADLO=$((PADLO + 1))
  fi
}

# preveri_v DEL OPIS BESEDILO  -> DEL je "skupaj" ali "naPorcijo"
preveri_v() {
  local del
  del=$(echo "$TELO" | grep -o "\"$1\":{[^}]*}")
  if echo "$del" | grep -qF "$3"; then
    echo "  ✔ $2"; USPELO=$((USPELO + 1))
  else
    echo "  ✘ $2 (v \"$1\" ni $3)"
    echo "    $1: $del"
    PADLO=$((PADLO + 1))
  fi
}

stevilka() { echo "$TELO" | grep -o "\"$1\":[0-9]*" | head -1 | sed 's/.*://'; }
besedilo() { echo "$TELO" | grep -o "\"$1\":\"[^\"]*\"" | head -1 | sed 's/^[^:]*:"//; s/"$//'; }

echo "== UZ-3.2 Hranilna vrednost recepta — testiranje na $URL =="

klic GET /api/status
if [ "$KODA" != "200" ]; then
  echo "API ne teče na $URL. Najprej zaženi: docker compose up -d  in  cd PojejMe.Api && dotnet run"
  exit 1
fi

# --- priprava: uporabnika A in B ---
for U in a b; do
  klic POST /api/auth/registracija "{\"ime\":\"Tester $U\",\"email\":\"hv-$U-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
  klic POST /api/auth/prijava "{\"email\":\"hv-$U-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
  ZETON="$(besedilo zeton)"
  [ -n "$ZETON" ] || { echo "Prijava ni uspela ($KODA): $TELO"; exit 1; }
  if [ "$U" = "a" ]; then ZETON_A="$ZETON"; else ZETON_B="$ZETON"; fi
done

# --- priprava: živila z znanimi hranilnimi vrednostmi ---
zivilo() { # IME SKUPINA KCAL B OH M
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/zivila" \
    -H "Content-Type: application/json" -H "X-Admin-Kljuc: $ADMIN_KLJUC" \
    --data "{\"ime\":\"$1 $CAS\",\"skupinaId\":$2,\"energijaKcal\":$3,\"beljakovine\":$4,\"ogljikoviHidrati\":$5,\"mascobe\":$6}")
  TELO=$(cat "$TMP/telo")
  stevilka id
}
MOKA="$(zivilo "HV moka" 3 364 10 76 1)"
MLEKO="$(zivilo "HV mleko" 5 64 3.3 4.8 3.6)"
JAJCE="$(zivilo "HV jajce" 8 155 13 1.1 11)"
[ -n "$MOKA" ] && [ -n "$MLEKO" ] && [ -n "$JAJCE" ] || { echo "Testnih živil ni bilo mogoče dodati: $TELO"; exit 1; }
echo "Priprava: uporabnika A in B, živila moka=$MOKA mleko=$MLEKO jajce=$JAJCE"

KOR='["Zmesaj.","Speci."]'
SEST_A="[{\"ziviloId\":$MOKA,\"kolicina\":250,\"enota\":\"g\"},{\"ziviloId\":$MLEKO,\"kolicina\":0.5,\"enota\":\"l\"}]"
SEST_B="[{\"ziviloId\":$MOKA,\"kolicina\":1,\"enota\":\"kg\"}]"
SEST_C="[{\"ziviloId\":$MOKA,\"kolicina\":100,\"enota\":\"g\"},{\"ziviloId\":$JAJCE,\"kolicina\":2,\"enota\":\"kos\"}]"
JSON_A="{\"ime\":\"HV recept A\",\"steviloPorcij\":4,\"sestavine\":$SEST_A,\"koraki\":$KOR}"
JSON_B="{\"ime\":\"HV recept B\",\"steviloPorcij\":2,\"sestavine\":$SEST_B,\"koraki\":$KOR}"
JSON_C="{\"ime\":\"HV recept C\",\"steviloPorcij\":1,\"sestavine\":$SEST_C,\"koraki\":$KOR}"

klic POST /api/recepti "$JSON_A" "$ZETON_A"; RA="$(stevilka id)"
klic POST /api/recepti "$JSON_B" "$ZETON_A"; RB="$(stevilka id)"
klic POST /api/recepti "$JSON_C" "$ZETON_A"; RC="$(stevilka id)"
[ -n "$RA" ] && [ -n "$RB" ] && [ -n "$RC" ] || { echo "Recepti niso bili ustvarjeni: $TELO"; exit 1; }
echo "Priprava: recepti A=$RA B=$RB C=$RC"

echo
klic GET "/api/recepti/$RA/hranilna-vrednost" "" "$ZETON_A"
preveri "GET /api/recepti/$RA/hranilna-vrednost" 200

echo "1) Preveri s prikazom kalorij recepta"
preveri_v skupaj "kalorije skupaj = 1230 kcal" '"energijaKcal":1230'
echo "2) Preveri s prikazom beljakovin"
preveri_v skupaj "beljakovine skupaj = 41.5 g" '"beljakovine":41.5'
echo "3) Preveri s prikazom ogljikovih hidratov"
preveri_v skupaj "ogljikovi hidrati skupaj = 214 g" '"ogljikoviHidrati":214'
echo "4) Preveri s prikazom maščob"
preveri_v skupaj "maščobe skupaj = 20.5 g" '"mascobe":20.5'
echo "5) Preveri s prikazom hranilnih vrednosti na porcijo (4 porcije)"
preveri_v naPorcijo "kalorije na porcijo = 307.5 kcal" '"energijaKcal":307.5'
preveri_v naPorcijo "beljakovine na porcijo = 10.4 g" '"beljakovine":10.4'
preveri_v naPorcijo "ogljikovi hidrati na porcijo = 53.5 g" '"ogljikoviHidrati":53.5'
preveri_v naPorcijo "maščobe na porcijo = 5.1 g" '"mascobe":5.1'

echo "6) Preveri z recepti z različnimi hranilnimi vrednostmi (recept B: 1 kg moke, 2 porciji)"
klic GET "/api/recepti/$RB/hranilna-vrednost" "" "$ZETON_A"
preveri "GET /api/recepti/$RB/hranilna-vrednost" 200
preveri_v skupaj "kalorije skupaj = 3640 kcal" '"energijaKcal":3640'
preveri_v skupaj "beljakovine skupaj = 100 g" '"beljakovine":100'
preveri_v naPorcijo "kalorije na porcijo = 1820 kcal" '"energijaKcal":1820'
preveri_v naPorcijo "ogljikovi hidrati na porcijo = 380 g" '"ogljikoviHidrati":380'

echo "7) Dodatno"
klic GET "/api/recepti/$RC/hranilna-vrednost" "" "$ZETON_A"
preveri_v skupaj "sestavina v kosih ni upoštevana (samo 100 g moke = 364 kcal)" '"energijaKcal":364'
if echo "$TELO" | grep -q '"opozorila":\["'; then
  echo "  ✔ vrnjeno opozorilo za sestavino v kosih"; USPELO=$((USPELO + 1))
else
  echo "  ✘ ni opozorila za sestavino v kosih: $TELO"; PADLO=$((PADLO + 1))
fi
klic GET "/api/recepti/$RA/hranilna-vrednost" "" "$ZETON_B"
preveri "drug uporabnik ne vidi hranilne vrednosti tujega recepta" 404
klic GET "/api/recepti/999999/hranilna-vrednost" "" "$ZETON_A"
preveri "neobstoječ recept" 404
klic GET "/api/recepti/$RA/hranilna-vrednost"
preveri "brez prijave ni dostopa" 401

rm -rf "$TMP"
echo
echo "== Rezultat: $USPELO uspešnih, $PADLO neuspešnih =="
[ "$PADLO" = "0" ]
