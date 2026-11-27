#!/usr/bin/env bash
# Avtomatsko preverjanje pogojev zadovoljstva UZ-3.6 Iskanje receptov (#29)
#
# Uporaba:
#   1. docker compose up -d          (baza)
#   2. cd PojejMe.Api && dotnet run  (API, privzeto http://localhost:5165)
#   3. v drugem terminalu: bash testi/uz-3.6-iskanje-receptov.sh [URL]
#
# Prehranske omejitve iz šifranta: 1 Vegetarijansko, 2 Vegansko, 3 Brez svinjine, 4 Halal, 5 Košer
# Testni recepti (kcal na porcijo izračuna API iz sestavin):
#   Golaz      120 min, 500 g mesa (250 kcal/100 g), 2 porciji  -> 625 kcal, brez omejitev
#   Palacinke   30 min, 100 g moke + 200 ml mleka, 2 porciji    -> 246 kcal, Vegetarijansko
#   Smoothie     5 min, 200 g banane, 1 porcija                 -> 178 kcal, Vegetarijansko + Vegansko
#   Solata      10 min, 300 g zelenjave, 1 porcija              ->  60 kcal, Vegetarijansko + Vegansko

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
ok()     { echo "  ✔ $1"; USPELO=$((USPELO + 1)); }
napaka() { echo "  ✘ $1"; [ -n "$2" ] && echo "    $2"; PADLO=$((PADLO + 1)); }

stevilka() { echo "$TELO" | grep -o "\"$1\":[0-9]*" | head -1 | sed 's/.*://'; }
besedilo() { echo "$TELO" | grep -o "\"$1\":\"[^\"]*\"" | head -1 | sed 's/^[^:]*:"//; s/"$//'; }

# imena najdenih testnih receptov v vrstnem redu odgovora
najdeni() { echo "$TELO" | grep -o '"ime":"IR [A-Za-z]* '"$CAS"'"' | sed 's/"ime":"IR //; s/ .*//' | tr '\n' ' '; }

# isci OPIS QUERY PRICAKOVANO  -> GET /api/recepti/iskanje?QUERY in primerja najdene recepte
isci() {
  klic GET "/api/recepti/iskanje?$2" "" "$ZETON"
  local n
  n="$(najdeni)"
  if [ "$KODA" = "200" ] && [ "$n" = "$3" ]; then
    ok "$1 → ${3:-(nič)}"
  else
    napaka "$1: pričakovano '${3:-(nič)}', dobljeno '$n' (HTTP $KODA)" "$(echo "$TELO" | head -c 300)"
  fi
}

echo "== UZ-3.6 Iskanje receptov — testiranje na $URL =="

klic GET /api/status
if [ "$KODA" != "200" ]; then
  echo "API ne teče na $URL. Najprej zaženi: docker compose up -d  in  cd PojejMe.Api && dotnet run"
  exit 1
fi

klic POST /api/auth/registracija "{\"ime\":\"Tester 36\",\"email\":\"ir-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
klic POST /api/auth/prijava "{\"email\":\"ir-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
ZETON="$(besedilo zeton)"
[ -n "$ZETON" ] || { echo "Prijava ni uspela ($KODA): $TELO"; exit 1; }

zivilo() { # IME SKUPINA KCAL
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/zivila" \
    -H "Content-Type: application/json" -H "X-Admin-Kljuc: $ADMIN_KLJUC" \
    --data "{\"ime\":\"IR $1 $CAS\",\"skupinaId\":$2,\"energijaKcal\":$3}")
  TELO=$(cat "$TMP/telo")
  stevilka id
}
MESO="$(zivilo meso 6 250)"; MOKA="$(zivilo moka 3 364)"; MLEKO="$(zivilo mleko 5 64)"
BANANA="$(zivilo banana 1 89)"; ZELENJAVA="$(zivilo zelenjava 2 20)"
[ -n "$MESO" ] && [ -n "$MOKA" ] && [ -n "$MLEKO" ] && [ -n "$BANANA" ] && [ -n "$ZELENJAVA" ] \
  || { echo "Testnih živil ni bilo mogoče dodati: $TELO"; exit 1; }

recept_json() { # IME CAS PORCIJE OMEJITVE SESTAVINE
  echo "{\"ime\":\"IR $1 $CAS\",\"casPripraveMin\":$2,\"steviloPorcij\":$3,\"prehranskeOmejitveIds\":$4,\"sestavine\":$5,\"koraki\":[\"Pripravi.\"]}"
}
S_GOLAZ="[{\"ziviloId\":$MESO,\"kolicina\":500,\"enota\":\"g\"}]"
S_PALACINKE="[{\"ziviloId\":$MOKA,\"kolicina\":100,\"enota\":\"g\"},{\"ziviloId\":$MLEKO,\"kolicina\":200,\"enota\":\"ml\"}]"
S_SMOOTHIE="[{\"ziviloId\":$BANANA,\"kolicina\":200,\"enota\":\"g\"}]"
S_SOLATA="[{\"ziviloId\":$ZELENJAVA,\"kolicina\":300,\"enota\":\"g\"}]"

J="$(recept_json Golaz 120 2 '[]' "$S_GOLAZ")";            klic POST /api/recepti "$J" "$ZETON"; preveri "recept Golaz (120 min, 625 kcal)" 201
GOLAZ_ID="$(stevilka id)"
J="$(recept_json Palacinke 30 2 '[1]' "$S_PALACINKE")";    klic POST /api/recepti "$J" "$ZETON"; preveri "recept Palacinke (30 min, 246 kcal, vegetarijansko)" 201
if echo "$TELO" | grep -qF '"casPripraveMin":30,"prehranskeOmejitve":["Vegetarijansko"]'; then ok "recept vrne čas priprave in omejitve"; else napaka "v odgovoru ni časa priprave in omejitev" "$(echo "$TELO" | tail -c 200)"; fi
J="$(recept_json Smoothie 5 1 '[1,2]' "$S_SMOOTHIE")";     klic POST /api/recepti "$J" "$ZETON"; preveri "recept Smoothie (5 min, 178 kcal, vegetarijansko + vegansko)" 201
J="$(recept_json Solata 10 1 '[1,2]' "$S_SOLATA")";        klic POST /api/recepti "$J" "$ZETON"; preveri "recept Solata (10 min, 60 kcal, vegetarijansko + vegansko)" 201

echo
echo "1) Preveri z iskanjem receptov po imenu"
isci "ime=palac (del imena, male črke)" "ime=palac" "Palacinke "
isci "ime=SMOOTH (velike črke)"          "ime=SMOOTH" "Smoothie "
isci "ime=$CAS (vsi testni recepti, po abecedi)" "ime=$CAS" "Golaz Palacinke Smoothie Solata "

echo "2) Preveri s filtriranjem po času priprave"
isci "maxCas=15"  "ime=$CAS&maxCas=15"  "Smoothie Solata "
isci "maxCas=30"  "ime=$CAS&maxCas=30"  "Palacinke Smoothie Solata "

echo "3) Preveri s filtriranjem po številu kalorij (na porcijo)"
isci "maxKcal=200"            "ime=$CAS&maxKcal=200"            "Smoothie Solata "
isci "minKcal=500"            "ime=$CAS&minKcal=500"            "Golaz "
isci "minKcal=100&maxKcal=300" "ime=$CAS&minKcal=100&maxKcal=300" "Palacinke Smoothie "

echo "4) Preveri s filtriranjem po prehranskih omejitvah"
isci "omejitve=1 (vegetarijansko)" "ime=$CAS&omejitve=1" "Palacinke Smoothie Solata "
isci "omejitve=2 (vegansko)"       "ime=$CAS&omejitve=2" "Smoothie Solata "
isci "omejitve=1&omejitve=2 (oboje)" "ime=$CAS&omejitve=1&omejitve=2" "Smoothie Solata "
J="$(recept_json Golaz 120 2 '[3]' "$S_GOLAZ")"
klic PUT "/api/recepti/$GOLAZ_ID" "$J" "$ZETON"; preveri "urejanje: Golaz dobi omejitev Brez svinjine" 200
isci "omejitve=3 (brez svinjine) po urejanju" "ime=$CAS&omejitve=3" "Golaz "

echo "5) Preveri s kombinacijo več filtrov hkrati"
isci "omejitve=1 + maxCas=20 + maxKcal=100" "ime=$CAS&omejitve=1&maxCas=20&maxKcal=100" "Solata "
isci "ime=a + maxCas=60 + minKcal=150"      "ime=a&maxCas=60&minKcal=150"                "Palacinke "

echo "6) Preveri z iskanjem recepta, ki ne obstaja"
isci "ime=neobstojec$CAS" "ime=neobstojec$CAS" ""
if [ "$TELO" = "[]" ]; then ok "odgovor je prazen seznam []"; else napaka "pričakovan prazen seznam" "$TELO"; fi
isci "kombinacija brez zadetkov (vegansko + minKcal=500)" "ime=$CAS&omejitve=2&minKcal=500" ""

echo "7) Dodatno"
klic GET "/api/recepti/iskanje?minKcal=500&maxKcal=100" "" "$ZETON"
preveri "minKcal > maxKcal je zavrnjeno" 400
klic GET "/api/recepti/iskanje?maxCas=0" "" "$ZETON"
preveri "maxCas=0 je zavrnjeno" 400
J="$(recept_json Napacen 0 1 '[]' "$S_SOLATA")"; klic POST /api/recepti "$J" "$ZETON"
preveri "čas priprave 0 min je zavrnjen" 400
J="$(recept_json Napacen 10 1 '[999]' "$S_SOLATA")"; klic POST /api/recepti "$J" "$ZETON"
preveri "neobstoječa prehranska omejitev je zavrnjena" 400
klic GET "/api/recepti/iskanje?ime=a"
preveri "brez prijave ni dostopa" 401

rm -rf "$TMP"
echo
echo "== Rezultat: $USPELO uspešnih, $PADLO neuspešnih =="
[ "$PADLO" = "0" ]
