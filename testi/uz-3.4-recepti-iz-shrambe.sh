#!/usr/bin/env bash
# Avtomatsko preverjanje pogojev zadovoljstva UZ-3.4 Recepti iz shrambe (#27)
#
# Uporaba:
#   1. docker compose up -d          (baza)
#   2. cd PojejMe.Api && dotnet run  (API, privzeto http://localhost:5165)
#   3. v drugem terminalu: bash testi/uz-3.4-recepti-iz-shrambe.sh [URL]
#
# Shramba:  moka 1 kg, mleko 1 l, jajca 6 kos, sladkor 100 g (rok je že potekel -> ne šteje)
# Recepti:
#   Palacinke: moka 250 g, mleko 500 ml, jajca 2 kos  -> 3/3 = 100 %, ima vse
#   Pecivo:    moka 2 kg, jajca 3 kos                 -> 1/2 =  50 %, manjka moka (v shrambi 1 kg)
#   Kruh:      moka 500 g, kvas 20 g, sladkor 10 g    -> 1/3 =  33 %, manjkata kvas in sladkor
#   Sirup:     sladkor 50 g                           -> nič iz shrambe, privzeto ni predlagan

URL="${1:-http://localhost:5165}"
ADMIN_KLJUC="lokalni-admin-kljuc"
TMP="$(mktemp -d)"
USPELO=0
PADLO=0
CAS="$(date +%s)"
VCERAJ="$(date -v-1d +%F 2>/dev/null || date -d yesterday +%F)"

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

ok()   { echo "  ✔ $1"; USPELO=$((USPELO + 1)); }
napaka() { echo "  ✘ $1"; [ -n "$2" ] && echo "    $2"; PADLO=$((PADLO + 1)); }

# povzetek predloga za recept IME: "odstotekNaVoljo":X,"imamVse":Y ...
predlog() {
  echo "$TELO" | grep -o "\"ime\":\"$1\",\"steviloSestavin\":[0-9]*,\"sestavinNaVoljo\":[0-9]*,\"odstotekNaVoljo\":[0-9]*,\"imamVse\":[a-z]*" | head -1
}
# celoten predlog z naVoljo in manjka
predlog_celoten() {
  echo "$TELO" | grep -o "\"ime\":\"$1\"[^]]*\][^]]*\]" | head -1
}

stevilka() { echo "$TELO" | grep -o "\"$1\":[0-9]*" | head -1 | sed 's/.*://'; }
besedilo() { echo "$TELO" | grep -o "\"$1\":\"[^\"]*\"" | head -1 | sed 's/^[^:]*:"//; s/"$//'; }

echo "== UZ-3.4 Recepti iz shrambe — testiranje na $URL =="

klic GET /api/status
if [ "$KODA" != "200" ]; then
  echo "API ne teče na $URL. Najprej zaženi: docker compose up -d  in  cd PojejMe.Api && dotnet run"
  exit 1
fi

klic POST /api/auth/registracija "{\"ime\":\"Tester 34\",\"email\":\"is-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
UPORABNIK_ID="$(stevilka id)"
klic POST /api/auth/prijava "{\"email\":\"is-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
ZETON="$(besedilo zeton)"
[ -n "$ZETON" ] && [ -n "$UPORABNIK_ID" ] || { echo "Registracija/prijava ni uspela ($KODA): $TELO"; exit 1; }

zivilo() { # IME SKUPINA
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/zivila" \
    -H "Content-Type: application/json" -H "X-Admin-Kljuc: $ADMIN_KLJUC" \
    --data "{\"ime\":\"$1 $CAS\",\"skupinaId\":$2,\"energijaKcal\":100}")
  TELO=$(cat "$TMP/telo")
  stevilka id
}
MOKA="$(zivilo "IS moka" 3)"; MLEKO="$(zivilo "IS mleko" 5)"; JAJCE="$(zivilo "IS jajce" 8)"
SLADKOR="$(zivilo "IS sladkor" 11)"; KVAS="$(zivilo "IS kvas" 14)"
[ -n "$MOKA" ] && [ -n "$MLEKO" ] && [ -n "$JAJCE" ] && [ -n "$SLADKOR" ] && [ -n "$KVAS" ] || { echo "Testnih živil ni bilo mogoče dodati: $TELO"; exit 1; }
echo "Priprava: uporabnik $UPORABNIK_ID, živila moka=$MOKA mleko=$MLEKO jajce=$JAJCE sladkor=$SLADKOR kvas=$KVAS"

v_shrambo() { # ZIVILO KOLICINA ENOTA LOKACIJA [ROK]
  local rok=""
  [ -n "$5" ] && rok=",\"rokUporabnosti\":\"$5\""
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/shramba" \
    -H "Content-Type: application/json" -H "X-Uporabnik-Id: $UPORABNIK_ID" \
    --data "{\"ziviloId\":$1,\"kolicina\":$2,\"enota\":\"$3\",\"lokacija\":\"$4\"$rok}")
  TELO=$(cat "$TMP/telo")
}

recept() { # IME SESTAVINE_JSON
  echo "{\"ime\":\"$1\",\"steviloPorcij\":2,\"sestavine\":$2,\"koraki\":[\"Pripravi.\"]}"
}
S_PALACINKE="[{\"ziviloId\":$MOKA,\"kolicina\":250,\"enota\":\"g\"},{\"ziviloId\":$MLEKO,\"kolicina\":500,\"enota\":\"ml\"},{\"ziviloId\":$JAJCE,\"kolicina\":2,\"enota\":\"kos\"}]"
S_PECIVO="[{\"ziviloId\":$MOKA,\"kolicina\":2,\"enota\":\"kg\"},{\"ziviloId\":$JAJCE,\"kolicina\":3,\"enota\":\"kos\"}]"
S_KRUH="[{\"ziviloId\":$MOKA,\"kolicina\":500,\"enota\":\"g\"},{\"ziviloId\":$KVAS,\"kolicina\":20,\"enota\":\"g\"},{\"ziviloId\":$SLADKOR,\"kolicina\":10,\"enota\":\"g\"}]"
S_SIRUP="[{\"ziviloId\":$SLADKOR,\"kolicina\":50,\"enota\":\"g\"}]"
I_PALACINKE="IS Palacinke $CAS"; I_PECIVO="IS Pecivo $CAS"; I_KRUH="IS Kruh $CAS"; I_SIRUP="IS Sirup $CAS"

echo
echo "1) Preveri z dodanimi sestavinami v shrambi"
v_shrambo "$MOKA" 1 kg Shramba;          preveri "moka 1 kg v shrambo" 201
v_shrambo "$MLEKO" 1 l Hladilnik;        preveri "mleko 1 l v shrambo" 201
v_shrambo "$JAJCE" 6 kos Hladilnik;      preveri "jajca 6 kos v shrambo" 201
v_shrambo "$SLADKOR" 100 g Shramba "$VCERAJ"; preveri "sladkor 100 g s preteklim rokom" 201

J="$(recept "$I_PALACINKE" "$S_PALACINKE")"; klic POST /api/recepti "$J" "$ZETON"; preveri "recept Palacinke" 201
J="$(recept "$I_PECIVO" "$S_PECIVO")";       klic POST /api/recepti "$J" "$ZETON"; preveri "recept Pecivo" 201
J="$(recept "$I_KRUH" "$S_KRUH")";           klic POST /api/recepti "$J" "$ZETON"; preveri "recept Kruh" 201
J="$(recept "$I_SIRUP" "$S_SIRUP")";         klic POST /api/recepti "$J" "$ZETON"; preveri "recept Sirup" 201

echo "2) Preveri z recepti, ki vsebujejo sestavine iz shrambe"
klic GET /api/recepti/iz-shrambe "" "$ZETON"
preveri "GET /api/recepti/iz-shrambe" 200
for R in "$I_PALACINKE" "$I_PECIVO" "$I_KRUH"; do
  if [ -n "$(predlog "$R")" ]; then ok "predlagan: $R"; else napaka "ni predlagan: $R" "$TELO"; fi
done
if echo "$TELO" | grep -qF "\"ime\":\"$I_SIRUP\""; then
  napaka "Sirup ne bi smel biti predlagan (nobene sestavine iz shrambe)"
else
  ok "Sirup ni predlagan (edina sestavina je sladkor s preteklim rokom)"
fi

echo "3) Preveri z izračunom odstotka sestavin recepta, ki jih uporabnik že ima"
P="$(predlog "$I_KRUH")"
if echo "$P" | grep -qF '"steviloSestavin":3,"sestavinNaVoljo":1,"odstotekNaVoljo":33'; then ok "Kruh: 1 od 3 sestavin = 33 %"; else napaka "Kruh: pričakovano 1/3 = 33 %" "$P"; fi
P="$(predlog "$I_PECIVO")"
if echo "$P" | grep -qF '"steviloSestavin":2,"sestavinNaVoljo":1,"odstotekNaVoljo":50'; then ok "Pecivo: 1 od 2 sestavin = 50 %"; else napaka "Pecivo: pričakovano 1/2 = 50 %" "$P"; fi

echo "4) Preveri z receptom, pri katerem ima uporabnik vse sestavine"
P="$(predlog "$I_PALACINKE")"
if echo "$P" | grep -qF '"steviloSestavin":3,"sestavinNaVoljo":3,"odstotekNaVoljo":100,"imamVse":true'; then
  ok "Palacinke: 3/3 = 100 %, imamVse = true (1 kg moke pokrije 250 g, 1 l mleka pokrije 500 ml)"
else napaka "Palacinke: pričakovano 100 % in imamVse = true" "$P"; fi

echo "5) Preveri z receptom, pri katerem uporabniku manjkajo določene sestavine"
P="$(predlog_celoten "$I_KRUH")"
if echo "$P" | grep -qF "\"zivilo\":\"IS kvas $CAS\"" && echo "$P" | grep -qF "\"zivilo\":\"IS sladkor $CAS\""; then
  ok "Kruh: manjkata kvas in sladkor (sladkor ima pretekel rok)"
else napaka "Kruh: v manjka ni kvasa in sladkorja" "$P"; fi
P="$(predlog_celoten "$I_PECIVO")"
if echo "$P" | grep -qF "\"zivilo\":\"IS moka $CAS\",\"potrebno\":2" && echo "$P" | grep -qF '"vShrambi":1'; then
  ok "Pecivo: manjka moka (potrebno 2 kg, v shrambi 1 kg)"
else napaka "Pecivo: pričakovano manjka moka 2 kg, v shrambi 1 kg" "$P"; fi
P="$(predlog "$I_PECIVO")"
if echo "$P" | grep -qF '"imamVse":false'; then ok "Pecivo: imamVse = false"; else napaka "Pecivo: imamVse bi moral biti false" "$P"; fi

echo "6) Preveri s pravilnim prikazom odstotka razpoložljivih sestavin"
VRSTNI_RED="$(echo "$TELO" | grep -o '"ime":"IS [A-Za-z]* '"$CAS"'","steviloSestavin"' | sed 's/"ime":"IS //; s/ .*//' | tr '\n' ' ')"
if [ "$VRSTNI_RED" = "Palacinke Pecivo Kruh " ]; then
  ok "razvrščeno po odstotku: Palacinke (100 %), Pecivo (50 %), Kruh (33 %)"
else napaka "napačen vrstni red: $VRSTNI_RED"; fi
klic GET "/api/recepti/iz-shrambe?vsi=true" "" "$ZETON"
P="$(predlog "$I_SIRUP")"
if echo "$P" | grep -qF '"odstotekNaVoljo":0,"imamVse":false'; then ok "?vsi=true: Sirup prikazan z 0 %"; else napaka "?vsi=true: Sirup bi moral imeti 0 %" "$P"; fi

echo "7) Dodatno"
klic GET /api/recepti/iz-shrambe
preveri "brez prijave ni dostopa" 401

rm -rf "$TMP"
echo
echo "== Rezultat: $USPELO uspešnih, $PADLO neuspešnih =="
[ "$PADLO" = "0" ]
