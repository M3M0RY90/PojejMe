#!/usr/bin/env bash
# Avtomatsko preverjanje pogojev zadovoljstva UZ-3.8 Manjkajoče sestavine (#31)
#
# Uporaba:
#   1. docker compose up -d          (baza)
#   2. cd PojejMe.Api && dotnet run  (API, privzeto http://localhost:5165)
#   3. v drugem terminalu: bash testi/uz-3.8-manjkajoce-sestavine.sh [URL]
#
# Shramba: moka 1 kg, mleko 200 ml, jajca 6 kos
# Recepti:
#   Kruh      (moka 500 g, kvas 20 g, sladkor 10 g) -> manjkata kvas 20 g in sladkor 10 g
#   Palacinke (moka 250 g, mleko 500 ml, jajca 2)   -> manjka mleko: potrebno 500 ml, v shrambi 200, dokupiti 300 ml
#   Omleta    (jajca 3 kos)                         -> ima vse, na seznam se ne doda nič

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
vsebuje()    { if echo "$TELO" | grep -qF "$2"; then ok "$1"; else napaka "$1 (ni: $2)" "$(echo "$TELO" | head -c 400)"; fi; }
ne_vsebuje() { if echo "$TELO" | grep -qF "$2"; then napaka "$1 (ne bi smelo biti: $2)" "$(echo "$TELO" | head -c 400)"; else ok "$1"; fi; }

stevilka() { echo "$TELO" | grep -o "\"$1\":[0-9]*" | head -1 | sed 's/.*://'; }
besedilo() { echo "$TELO" | grep -o "\"$1\":\"[^\"]*\"" | head -1 | sed 's/^[^:]*:"//; s/"$//'; }

# postavke nakupovalnega seznama za živilo, npr. "20.00 g"
na_seznamu() {
  echo "$SEZNAM" | grep -o "\"zivilo\":\"MS $1 $CAS\",\"kolicina\":[0-9.]*,\"enota\":\"[a-z]*\"" \
    | sed 's/.*"kolicina":\([0-9.]*\),"enota":"\([a-z]*\)"/\1 \2/' | tr '\n' ' ' | sed 's/ $//'
}
nalozi_seznam() { klic GET /api/nakupovalni-seznam "" "$ZETON"; SEZNAM="$TELO"; }
preveri_seznam() { # ZIVILO PRICAKOVANO OPIS
  local p; p="$(na_seznamu "$1")"
  if [ "$p" = "$2" ]; then ok "$3: $1 ${2:-(ni na seznamu)}"; else napaka "$3: $1 pričakovano '${2:-(ni na seznamu)}', na seznamu '$p'"; fi
}

echo "== UZ-3.8 Manjkajoče sestavine — testiranje na $URL =="

klic GET /api/status
if [ "$KODA" != "200" ]; then
  echo "API ne teče na $URL. Najprej zaženi: docker compose up -d  in  cd PojejMe.Api && dotnet run"
  exit 1
fi

klic POST /api/auth/registracija "{\"ime\":\"Tester 38\",\"email\":\"ms-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
UPORABNIK_ID="$(stevilka id)"
klic POST /api/auth/prijava "{\"email\":\"ms-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
ZETON="$(besedilo zeton)"
[ -n "$ZETON" ] && [ -n "$UPORABNIK_ID" ] || { echo "Registracija/prijava ni uspela ($KODA): $TELO"; exit 1; }

zivilo() { # IME SKUPINA
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/zivila" \
    -H "Content-Type: application/json" -H "X-Admin-Kljuc: $ADMIN_KLJUC" \
    --data "{\"ime\":\"MS $1 $CAS\",\"skupinaId\":$2,\"energijaKcal\":100}")
  TELO=$(cat "$TMP/telo")
  stevilka id
}
MOKA="$(zivilo moka 3)"; MLEKO="$(zivilo mleko 5)"; JAJCA="$(zivilo jajca 8)"
KVAS="$(zivilo kvas 14)"; SLADKOR="$(zivilo sladkor 11)"
[ -n "$MOKA" ] && [ -n "$MLEKO" ] && [ -n "$JAJCA" ] && [ -n "$KVAS" ] && [ -n "$SLADKOR" ] \
  || { echo "Testnih živil ni bilo mogoče dodati: $TELO"; exit 1; }

v_shrambo() { # ZIVILO KOLICINA ENOTA OPIS
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/shramba" \
    -H "Content-Type: application/json" -H "X-Uporabnik-Id: $UPORABNIK_ID" \
    --data "{\"ziviloId\":$1,\"kolicina\":$2,\"enota\":\"$3\",\"lokacija\":\"Shramba\"}")
  TELO=$(cat "$TMP/telo")
  preveri "$4" 201
}
recept() { # IME SESTAVINE
  local json="{\"ime\":\"MS $1 $CAS\",\"steviloPorcij\":2,\"sestavine\":$2,\"koraki\":[\"Pripravi.\"]}"
  klic POST /api/recepti "$json" "$ZETON"
  preveri "recept $1" 201
}

v_shrambo "$MOKA" 1 kg "shramba: moka 1 kg"
v_shrambo "$MLEKO" 200 ml "shramba: mleko 200 ml"
v_shrambo "$JAJCA" 6 kos "shramba: jajca 6 kos"
recept Kruh "[{\"ziviloId\":$MOKA,\"kolicina\":500,\"enota\":\"g\"},{\"ziviloId\":$KVAS,\"kolicina\":20,\"enota\":\"g\"},{\"ziviloId\":$SLADKOR,\"kolicina\":10,\"enota\":\"g\"}]"
KRUH="$(stevilka id)"
recept Palacinke "[{\"ziviloId\":$MOKA,\"kolicina\":250,\"enota\":\"g\"},{\"ziviloId\":$MLEKO,\"kolicina\":500,\"enota\":\"ml\"},{\"ziviloId\":$JAJCA,\"kolicina\":2,\"enota\":\"kos\"}]"
PALACINKE="$(stevilka id)"
recept Omleta "[{\"ziviloId\":$JAJCA,\"kolicina\":3,\"enota\":\"kos\"}]"
OMLETA="$(stevilka id)"

echo
echo "1) Preveri z receptom, ki vsebuje sestavine, ki jih uporabnik nima v shrambi"
klic GET "/api/recepti/$KRUH/manjkajoce" "" "$ZETON"
preveri "GET /api/recepti/$KRUH/manjkajoce (Kruh)" 200
vsebuje "Kruh: imamVse = false" '"imamVse":false'

echo "2) Preveri s pravilno identifikacijo manjkajočih sestavin"
vsebuje "manjka kvas (dokupiti 20 g)" "\"zivilo\":\"MS kvas $CAS\",\"potrebno\":20.00,\"vShrambi\":0,\"dokupiti\":20"
vsebuje "manjka sladkor (dokupiti 10 g)" "\"zivilo\":\"MS sladkor $CAS\",\"potrebno\":10.00,\"vShrambi\":0,\"dokupiti\":10"
ne_vsebuje "moka ne manjka (1 kg v shrambi pokrije 500 g)" "\"zivilo\":\"MS moka $CAS\""
klic GET "/api/recepti/$PALACINKE/manjkajoce" "" "$ZETON"
vsebuje "Palacinke: manjka samo del mleka (potrebno 500 ml, v shrambi 200 ml, dokupiti 300 ml)" \
  "\"zivilo\":\"MS mleko $CAS\",\"potrebno\":500.00,\"vShrambi\":200.00,\"dokupiti\":300.00"

echo "3) Preveri z dodajanjem manjkajočih sestavin na nakupovalni seznam"
klic POST "/api/recepti/$KRUH/manjkajoce-na-seznam" "" "$ZETON"
preveri "POST /api/recepti/$KRUH/manjkajoce-na-seznam" 200
vsebuje "dodana kvas in sladkor" '"sporocilo":"Na nakupovalni seznam dodanih sestavin: 2,'
nalozi_seznam
preveri "GET /api/nakupovalni-seznam" 200
preveri_seznam kvas "20.00 g" "na seznamu"
preveri_seznam sladkor "10.00 g" "na seznamu"

echo "4) Preveri z receptom, za katerega ima uporabnik vse potrebne sestavine"
klic GET "/api/recepti/$OMLETA/manjkajoce" "" "$ZETON"
vsebuje "Omleta: imamVse = true, nič ne manjka" '"imamVse":true,"manjka":[]'
klic POST "/api/recepti/$OMLETA/manjkajoce-na-seznam" "" "$ZETON"
preveri "POST za Omleto" 200
vsebuje "nič ni dodano" '"dodano":[],"zeNaSeznamu":[]'
nalozi_seznam
preveri_seznam jajca "" "jajc ni na seznamu"

echo "5) Preveri, da se na seznam dodajo samo manjkajoče sestavine"
klic POST "/api/recepti/$PALACINKE/manjkajoce-na-seznam" "" "$ZETON"
preveri "POST za Palacinke" 200
nalozi_seznam
preveri_seznam moka "" "moke ni na seznamu (je v shrambi)"
preveri_seznam jajca "" "jajc ni na seznamu (so v shrambi)"
preveri_seznam mleko "300.00 ml" "na seznamu samo manjkajoče mleko"
klic POST "/api/recepti/$KRUH/manjkajoce-na-seznam" "" "$ZETON"
vsebuje "ponovno dodajanje: kvas in sladkor sta že na seznamu" '"sporocilo":"Na nakupovalni seznam dodanih sestavin: 0,'
nalozi_seznam
preveri_seznam kvas "20.00 g" "kvas je na seznamu samo enkrat"

echo "6) Preveri s prikazom potrebnih količin posamezne sestavine na seznamu"
preveri_seznam kvas "20.00 g" "količina"
preveri_seznam sladkor "10.00 g" "količina"
preveri_seznam mleko "300.00 ml" "količina (samo razlika do potrebne)"
if echo "$SEZNAM" | grep -qF "\"zivilo\":\"MS kvas $CAS\",\"kolicina\":20.00,\"enota\":\"g\",\"izReceptaId\":$KRUH"; then
  ok "postavka ve, iz katerega recepta je bila dodana"
else napaka "postavka nima izReceptaId" "$SEZNAM"; fi

echo "7) Dodatno"
klic GET "/api/recepti/999999/manjkajoce" "" "$ZETON"
preveri "neobstoječ recept" 404
klic POST "/api/recepti/$KRUH/manjkajoce-na-seznam"
preveri "brez prijave ni dostopa" 401
klic GET /api/nakupovalni-seznam
preveri "seznam brez prijave ni dostopen" 401

rm -rf "$TMP"
echo
echo "== Rezultat: $USPELO uspešnih, $PADLO neuspešnih =="
[ "$PADLO" = "0" ]
