#!/usr/bin/env bash
# Avtomatsko preverjanje pogojev zadovoljstva UZ-3.7 Skuhal sem (#30)
#
# Uporaba:
#   1. docker compose up -d          (baza)
#   2. cd PojejMe.Api && dotnet run  (API, privzeto http://localhost:5165)
#   3. v drugem terminalu: bash testi/uz-3.7-skuhal-sem.sh [URL]
#
# Shramba: moka 1 kg (rok +30 dni), moka 200 g (rok +2 dni), mleko 1 l, jajca 6 kos
# Recept Palacinke (2 porciji): moka 300 g, mleko 500 ml, jajca 2 kos
#   1. priprava (2 porciji, ključ k1): najprej se porabi moka z bližjim rokom (200 g -> odstranjena),
#      nato 100 g iz 1 kg -> moka 0.90 kg, mleko 0.50 l, jajca 4 kos
#   2. ponovitev s ključem k1: nič se ne odšteje
#   3. priprava 1 porcije: moka 0.75 kg, mleko 0.25 l, jajca 3 kos
# Recept Torta: moka 300 g, maslo 250 g (masla ni v shrambi)
#   brez kljubTemu -> 409, shramba ostane enaka; s kljubTemu -> moka 0.45 kg, maslo med manjkajočimi

URL="${1:-http://localhost:5165}"
ADMIN_KLJUC="lokalni-admin-kljuc"
TMP="$(mktemp -d)"
USPELO=0
PADLO=0
CAS="$(date +%s)"

datum() {
  if date -u -v+0d +%F >/dev/null 2>&1; then date -u -v+"$1"d +%F; else date -u -d "$1 day" +%F; fi
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

# trenutne zaloge živila v shrambi, npr. "0.90 kg" (več zalog ločenih s presledkom)
SHRAMBA=""
nalozi_shrambo() {
  SHRAMBA=$(curl -s "$URL/api/shramba" -H "X-Uporabnik-Id: $UPORABNIK_ID")
}
zaloga() {
  echo "$SHRAMBA" | grep -o "\"zivilo\":\"SS $1 $CAS\",\"kolicina\":[0-9.]*,\"enota\":\"[a-z]*\"" \
    | sed 's/.*"kolicina":\([0-9.]*\),"enota":"\([a-z]*\)"/\1 \2/' | tr '\n' ' ' | sed 's/ $//'
}
preveri_zalogo() { # ZIVILO PRICAKOVANO OPIS
  local z; z="$(zaloga "$1")"
  if [ "$z" = "$2" ]; then ok "$3: $1 = ${2:-(ni več v shrambi)}"; else napaka "$3: $1 pričakovano '${2:-(ni več)}', v shrambi '$z'"; fi
}

echo "== UZ-3.7 Skuhal sem — testiranje na $URL =="

klic GET /api/status
if [ "$KODA" != "200" ]; then
  echo "API ne teče na $URL. Najprej zaženi: docker compose up -d  in  cd PojejMe.Api && dotnet run"
  exit 1
fi

klic POST /api/auth/registracija "{\"ime\":\"Tester 37\",\"email\":\"ss-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
UPORABNIK_ID="$(stevilka id)"
klic POST /api/auth/prijava "{\"email\":\"ss-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
ZETON="$(besedilo zeton)"
[ -n "$ZETON" ] && [ -n "$UPORABNIK_ID" ] || { echo "Registracija/prijava ni uspela ($KODA): $TELO"; exit 1; }

zivilo() { # IME SKUPINA
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/zivila" \
    -H "Content-Type: application/json" -H "X-Admin-Kljuc: $ADMIN_KLJUC" \
    --data "{\"ime\":\"SS $1 $CAS\",\"skupinaId\":$2,\"energijaKcal\":100}")
  TELO=$(cat "$TMP/telo")
  stevilka id
}
MOKA="$(zivilo moka 3)"; MLEKO="$(zivilo mleko 5)"; JAJCE="$(zivilo jajca 8)"; MASLO="$(zivilo maslo 10)"
[ -n "$MOKA" ] && [ -n "$MLEKO" ] && [ -n "$JAJCE" ] && [ -n "$MASLO" ] || { echo "Testnih živil ni bilo mogoče dodati: $TELO"; exit 1; }

v_shrambo() { # ZIVILO KOLICINA ENOTA OPIS [ROK_ZAMIK]
  local rok=""
  [ -n "$5" ] && rok=",\"rokUporabnosti\":\"$(datum "$5")\""
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/shramba" \
    -H "Content-Type: application/json" -H "X-Uporabnik-Id: $UPORABNIK_ID" \
    --data "{\"ziviloId\":$1,\"kolicina\":$2,\"enota\":\"$3\",\"lokacija\":\"Shramba\"$rok}")
  TELO=$(cat "$TMP/telo")
  preveri "$4" 201
}

echo
echo "2) Preveri z receptom, katerega sestavine so v shrambi"
v_shrambo "$MOKA" 1 kg "moka 1 kg (rok čez 30 dni)" 30
v_shrambo "$MOKA" 200 g "moka 200 g (rok čez 2 dni)" 2
v_shrambo "$MLEKO" 1 l "mleko 1 l"
v_shrambo "$JAJCE" 6 kos "jajca 6 kos"
J="{\"ime\":\"SS Palacinke $CAS\",\"steviloPorcij\":2,\"sestavine\":[{\"ziviloId\":$MOKA,\"kolicina\":300,\"enota\":\"g\"},{\"ziviloId\":$MLEKO,\"kolicina\":500,\"enota\":\"ml\"},{\"ziviloId\":$JAJCE,\"kolicina\":2,\"enota\":\"kos\"}],\"koraki\":[\"Speci.\"]}"
klic POST /api/recepti "$J" "$ZETON"; preveri "recept Palacinke (moka 300 g, mleko 500 ml, jajca 2 kos)" 201
PALACINKE="$(stevilka id)"
J="{\"ime\":\"SS Torta $CAS\",\"steviloPorcij\":8,\"sestavine\":[{\"ziviloId\":$MOKA,\"kolicina\":300,\"enota\":\"g\"},{\"ziviloId\":$MASLO,\"kolicina\":250,\"enota\":\"g\"}],\"koraki\":[\"Speci.\"]}"
klic POST /api/recepti "$J" "$ZETON"; preveri "recept Torta (moka 300 g, maslo 250 g)" 201
TORTA="$(stevilka id)"

echo "1) Preveri z označitvijo recepta kot pripravljenega"
klic POST "/api/recepti/$PALACINKE/skuhal-sem" '{"kljuc":"k1"}' "$ZETON"
preveri "POST /api/recepti/$PALACINKE/skuhal-sem (ključ k1)" 201
if echo "$TELO" | grep -qF '"zeZabelezeno":false'; then ok "priprava je zabeležena"; else napaka "v odgovoru ni zeZabelezeno=false" "$TELO"; fi

echo "3) Preveri s samodejnim odštevanjem porabljenih količin sestavin"
for X in "moka:300" "mleko:500" "jajca:2"; do
  IME="${X%%:*}"; KOL="${X##*:}"
  if echo "$TELO" | grep -qF "\"zivilo\":\"SS $IME $CAS\",\"porabljeno\":$KOL"; then ok "porabljeno: $IME $KOL"; else napaka "v porabi ni $IME $KOL" "$TELO"; fi
done

echo "4) Preveri s posodobitvijo stanja shrambe po pripravi recepta"
nalozi_shrambo
preveri_zalogo moka "0.90 kg" "najprej porabljena moka z bližjim rokom (200 g odstranjena), nato 100 g iz 1 kg"
preveri_zalogo mleko "0.50 l" "500 ml odšteto od 1 l"
preveri_zalogo jajca "4.00 kos" "2 kosa odšteta od 6"

echo "6) Preveri, da se sestavine odštejejo samo enkrat na pripravo"
klic POST "/api/recepti/$PALACINKE/skuhal-sem" '{"kljuc":"k1"}' "$ZETON"
preveri "ponovljen zahtevek z istim ključem k1" 200
if echo "$TELO" | grep -qF '"zeZabelezeno":true,"poraba":[]'; then ok "odgovor: že zabeleženo, nič porabljeno"; else napaka "pričakovano zeZabelezeno=true in prazna poraba" "$TELO"; fi
nalozi_shrambo
preveri_zalogo moka "0.90 kg" "po ponovitvi se shramba ne spremeni"
preveri_zalogo jajca "4.00 kos" "po ponovitvi se shramba ne spremeni"
klic POST "/api/recepti/$PALACINKE/skuhal-sem" '{"steviloPorcij":1}' "$ZETON"
preveri "nova priprava (1 porcija, brez ključa)" 201
nalozi_shrambo
preveri_zalogo moka "0.75 kg" "nova priprava odšteje polovico (150 g)"
preveri_zalogo mleko "0.25 l" "nova priprava odšteje 250 ml"
preveri_zalogo jajca "3.00 kos" "nova priprava odšteje 1 kos"

echo "5) Preveri z receptom, za katerega uporabnik nima vseh sestavin"
klic POST "/api/recepti/$TORTA/skuhal-sem" "" "$ZETON"
preveri "Torta brez masla je zavrnjena" 409
if echo "$TELO" | grep -qF "\"zivilo\":\"SS maslo $CAS\""; then ok "odgovor navede manjkajoče maslo"; else napaka "v odgovoru ni manjkajočega masla" "$TELO"; fi
nalozi_shrambo
preveri_zalogo moka "0.75 kg" "po zavrnitvi se shramba ne spremeni"
klic POST "/api/recepti/$TORTA/skuhal-sem" '{"kljubTemu":true}' "$ZETON"
preveri "Torta s kljubTemu=true" 201
if echo "$TELO" | grep -qF "\"manjka\":[{\"ziviloId\":$MASLO"; then ok "maslo je med manjkajočimi"; else napaka "maslo bi moralo biti med manjkajočimi" "$TELO"; fi
nalozi_shrambo
preveri_zalogo moka "0.45 kg" "odšteto, kar je na voljo (moka 300 g)"

echo "7) Dodatno"
klic POST "/api/recepti/999999/skuhal-sem" "" "$ZETON"
preveri "neobstoječ recept" 404
klic POST "/api/recepti/$PALACINKE/skuhal-sem" '{"steviloPorcij":0}' "$ZETON"
preveri "0 porcij je zavrnjeno" 400
klic POST "/api/recepti/$PALACINKE/skuhal-sem"
preveri "brez prijave ni dostopa" 401

rm -rf "$TMP"
echo
echo "== Rezultat: $USPELO uspešnih, $PADLO neuspešnih =="
[ "$PADLO" = "0" ]
