#!/usr/bin/env bash
# Avtomatsko preverjanje pogojev zadovoljstva UZ-3.5 Reši hrano (#28)
#
# Uporaba:
#   1. docker compose up -d          (baza)
#   2. cd PojejMe.Api && dotnet run  (API, privzeto http://localhost:5165)
#   3. v drugem terminalu: bash testi/uz-3.5-resi-hrano.sh [URL]
#
# Shramba (rok glede na današnji dan):
#   jogurt +1 dan, banana +2 dni, smetana +3 dni   -> kritična (privzeto okno 3 dni)
#   moka +10 dni, sol brez roka                    -> dovolj dolg rok, niso kritična
#   mleko -1 dan                                   -> že pretečeno, ni več kritično
# Recepti:
#   Smoothie (jogurt, banana)  -> najbližji rok +1  -> 1. mesto
#   Bananin kruh (banana, moka)-> najbližji rok +2  -> 2. mesto
#   Omaka (smetana, sol)       -> najbližji rok +3  -> 3. mesto
#   Kruh (moka, sol)           -> samo dolg rok     -> ni predlagan (z ?dni=10 pa je, na zadnjem mestu)
#   Napitek (mleko)            -> samo pretečeno    -> ni predlagan

URL="${1:-http://localhost:5165}"
ADMIN_KLJUC="lokalni-admin-kljuc"
TMP="$(mktemp -d)"
USPELO=0
PADLO=0
CAS="$(date +%s)"

# datum ZAMIK -> današnji datum (UTC, kot na strežniku) + ZAMIK dni, oblika YYYY-MM-DD
datum() {
  if date -u -v+0d +%F >/dev/null 2>&1; then
    if [ "$1" -ge 0 ]; then date -u -v+"$1"d +%F; else date -u -v"$1"d +%F; fi
  else
    date -u -d "$1 day" +%F
  fi
}

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

# del odgovora: seznam kritičnih živil oz. seznam receptov
zivila_del()  { echo "$TELO" | sed 's/,"recepti":.*//'; }
recepti_del() { echo "$TELO" | sed 's/.*"recepti"://'; }
# vrstni red receptov (samo imena brez predpone in časovne oznake)
vrstni_red() { recepti_del | grep -o '"ime":"RH [A-Za-z]* '"$CAS"'"' | sed 's/"ime":"RH //; s/ .*//' | tr '\n' ' '; }

echo "== UZ-3.5 Reši hrano — testiranje na $URL =="

klic GET /api/status
if [ "$KODA" != "200" ]; then
  echo "API ne teče na $URL. Najprej zaženi: docker compose up -d  in  cd PojejMe.Api && dotnet run"
  exit 1
fi

klic POST /api/auth/registracija "{\"ime\":\"Tester 35\",\"email\":\"rh-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
UPORABNIK_ID="$(stevilka id)"
klic POST /api/auth/prijava "{\"email\":\"rh-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
ZETON="$(besedilo zeton)"
[ -n "$ZETON" ] && [ -n "$UPORABNIK_ID" ] || { echo "Registracija/prijava ni uspela ($KODA): $TELO"; exit 1; }

zivilo() { # IME SKUPINA
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/zivila" \
    -H "Content-Type: application/json" -H "X-Admin-Kljuc: $ADMIN_KLJUC" \
    --data "{\"ime\":\"RH $1 $CAS\",\"skupinaId\":$2,\"energijaKcal\":100}")
  TELO=$(cat "$TMP/telo")
  stevilka id
}
JOGURT="$(zivilo jogurt 5)"; BANANA="$(zivilo banana 1)"; SMETANA="$(zivilo smetana 5)"
MOKA="$(zivilo moka 3)"; SOL="$(zivilo sol 13)"; MLEKO="$(zivilo mleko 5)"
[ -n "$JOGURT" ] && [ -n "$BANANA" ] && [ -n "$SMETANA" ] && [ -n "$MOKA" ] && [ -n "$SOL" ] && [ -n "$MLEKO" ] \
  || { echo "Testnih živil ni bilo mogoče dodati: $TELO"; exit 1; }
echo "Priprava: uporabnik $UPORABNIK_ID, danes (UTC) $(datum 0)"

v_shrambo() { # ZIVILO OPIS [ZAMIK_DNI]
  local rok=""
  [ -n "$3" ] && rok=",\"rokUporabnosti\":\"$(datum "$3")\""
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/shramba" \
    -H "Content-Type: application/json" -H "X-Uporabnik-Id: $UPORABNIK_ID" \
    --data "{\"ziviloId\":$1,\"kolicina\":1,\"enota\":\"kg\",\"lokacija\":\"Hladilnik\"$rok}")
  TELO=$(cat "$TMP/telo")
  preveri "$2" 201
}

recept() { # IME ZIVILO...
  local ime="$1"; shift
  local sest="" id
  for id in "$@"; do sest="$sest{\"ziviloId\":$id,\"kolicina\":100,\"enota\":\"g\"},"; done
  local json="{\"ime\":\"RH $ime $CAS\",\"steviloPorcij\":2,\"sestavine\":[${sest%,}],\"koraki\":[\"Pripravi.\"]}"
  klic POST /api/recepti "$json" "$ZETON"
  preveri "recept $ime" 201
}

echo
echo "1) Preveri z živili v shrambi, ki imajo določen rok uporabe"
v_shrambo "$JOGURT"  "jogurt, rok čez 1 dan" 1
v_shrambo "$BANANA"  "banana, rok čez 2 dni" 2
v_shrambo "$SMETANA" "smetana, rok čez 3 dni" 3
v_shrambo "$MOKA"    "moka, rok čez 10 dni" 10
v_shrambo "$SOL"     "sol, brez roka"
v_shrambo "$MLEKO"   "mleko, rok je potekel včeraj" -1
recept Smoothie "$JOGURT" "$BANANA"
recept Bananin  "$BANANA" "$MOKA"
recept Omaka    "$SMETANA" "$SOL"
recept Kruh     "$MOKA" "$SOL"
recept Napitek  "$MLEKO"

echo "2) Preveri z živili, ki jim rok uporabe poteče v naslednjih nekaj dneh"
klic GET /api/recepti/resi-hrano "" "$ZETON"
preveri "GET /api/recepti/resi-hrano (privzeto 3 dni)" 200
Z="$(zivila_del)"
for X in "jogurt:1" "banana:2" "smetana:3"; do
  IME="${X%%:*}"; DNI="${X##*:}"
  if echo "$Z" | grep -qF "\"zivilo\":\"RH $IME $CAS\",\"rokUporabnosti\":\"$(datum "$DNI")\",\"dniDoRoka\":$DNI"; then
    ok "$IME je kritično živilo (rok čez $DNI dni)"
  else napaka "$IME bi moral biti med kritičnimi živili (rok čez $DNI dni)" "$Z"; fi
done

echo "3) Preveri s prednostnim prikazom receptov, ki vsebujejo ta živila"
R="$(recepti_del)"
for IME in Smoothie Bananin Omaka; do
  if echo "$R" | grep -qF "\"ime\":\"RH $IME $CAS\""; then ok "predlagan recept $IME"; else napaka "recept $IME ni predlagan" "$R"; fi
done
if echo "$R" | grep -qF "\"ime\":\"RH Smoothie $CAS\",\"najblizjiRok\":\"$(datum 1)\",\"dniDoRoka\":1"; then
  ok "Smoothie: najbližji rok čez 1 dan (jogurt)"
else napaka "Smoothie: pričakovan najbližji rok čez 1 dan" "$R"; fi

echo "4) Preveri z živili, ki imajo še dovolj dolg rok uporabe"
for IME in moka sol mleko; do
  if echo "$Z" | grep -qF "\"zivilo\":\"RH $IME $CAS\""; then napaka "$IME ne bi smel biti med kritičnimi živili" "$Z"; else ok "$IME ni kritično živilo"; fi
done
for IME in Kruh Napitek; do
  if echo "$R" | grep -qF "\"ime\":\"RH $IME $CAS\""; then napaka "recept $IME ne bi smel biti predlagan" "$R"; else ok "recept $IME ni predlagan (nima živil s kratkim rokom)"; fi
done

echo "5) Preveri s pravilnim razvrščanjem receptov glede na rok uporabe sestavin"
VR="$(vrstni_red)"
if [ "$VR" = "Smoothie Bananin Omaka " ]; then ok "vrstni red: Smoothie (1 dan), Bananin kruh (2 dni), Omaka (3 dni)"; else napaka "napačen vrstni red: $VR"; fi
klic GET "/api/recepti/resi-hrano?dni=10" "" "$ZETON"
preveri "GET /api/recepti/resi-hrano?dni=10" 200
VR="$(vrstni_red)"
if [ "$VR" = "Smoothie Bananin Omaka Kruh " ]; then ok "z oknom 10 dni je Kruh (moka čez 10 dni) na zadnjem mestu"; else napaka "napačen vrstni red pri 10 dneh: $VR"; fi

echo "6) Dodatno"
klic GET "/api/recepti/resi-hrano?dni=0" "" "$ZETON"
preveri "neveljavno število dni (0) je zavrnjeno" 400
klic GET /api/recepti/resi-hrano
preveri "brez prijave ni dostopa" 401

rm -rf "$TMP"
echo
echo "== Rezultat: $USPELO uspešnih, $PADLO neuspešnih =="
[ "$PADLO" = "0" ]
