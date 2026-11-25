#!/usr/bin/env bash
# Avtomatsko preverjanje pogojev zadovoljstva UZ-3.1 Upravljanje receptov (#24)
#
# Uporaba:
#   1. docker compose up -d          (baza)
#   2. cd PojejMe.Api && dotnet run  (API, privzeto http://localhost:5165)
#   3. v drugem terminalu: bash testi/uz-3.1-recepti.sh [URL]
#
# Skripta ustvari dva testna uporabnika in dve testni živili, nato preveri vse pogoje.

URL="${1:-http://localhost:5165}"
ADMIN_KLJUC="lokalni-admin-kljuc"
TMP="$(mktemp -d)"
USPELO=0
PADLO=0
CAS="$(date +%s)"

# klic METODA POT [JSON] [ZETON]  -> nastavi KODA in TELO
klic() {
  local args=(-s -o "$TMP/telo" -w "%{http_code}" -X "$1" "$URL$2")
  [ -n "$3" ] && args+=(-H "Content-Type: application/json" --data "$3")
  [ -n "$4" ] && args+=(-H "Authorization: Bearer $4")
  KODA=$(curl "${args[@]}")
  TELO=$(cat "$TMP/telo" 2>/dev/null)
}

# nalozi POT DATOTEKA TIP ZETON  -> multipart nalaganje slike
nalozi() {
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL$1" \
    -H "Authorization: Bearer $4" -F "slika=@$2;type=$3")
  TELO=$(cat "$TMP/telo" 2>/dev/null)
}

# preveri OPIS PRICAKOVANA_KODA
preveri() {
  if [ "$KODA" = "$2" ]; then
    echo "  ✔ $1 (HTTP $KODA)"; USPELO=$((USPELO + 1))
  else
    echo "  ✘ $1 (pričakovano $2, dobljeno $KODA)"
    echo "    odgovor: $(echo "$TELO" | head -c 300)"
    PADLO=$((PADLO + 1))
  fi
}

# preveri_vsebuje OPIS BESEDILO
preveri_vsebuje() {
  if echo "$TELO" | grep -qF "$2"; then
    echo "  ✔ $1"; USPELO=$((USPELO + 1))
  else
    echo "  ✘ $1 (v odgovoru ni: $2)"
    echo "    odgovor: $(echo "$TELO" | head -c 300)"
    PADLO=$((PADLO + 1))
  fi
}

stevilka() { echo "$TELO" | grep -o "\"$1\":[0-9]*" | head -1 | sed 's/.*://'; }
besedilo() { echo "$TELO" | grep -o "\"$1\":\"[^\"]*\"" | head -1 | sed 's/^[^:]*:"//; s/"$//'; }

recept() { # IME PORCIJE SESTAVINE KORAKI
  printf '{"ime":"%s","opis":"Testni recept","steviloPorcij":%s,"sestavine":%s,"koraki":%s}' "$1" "$2" "$3" "$4"
}

echo "== UZ-3.1 Upravljanje receptov — testiranje na $URL =="

klic GET /api/status
if [ "$KODA" != "200" ]; then
  echo "API ne teče na $URL. Najprej zaženi: docker compose up -d  in  cd PojejMe.Api && dotnet run"
  exit 1
fi
echo "Status: $TELO"

# --- priprava: dva uporabnika ---
for U in a b; do
  klic POST /api/auth/registracija "{\"ime\":\"Tester $U\",\"email\":\"test-$U-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
  [ "$KODA" = "200" ] || [ "$KODA" = "201" ] || { echo "Registracija ni uspela ($KODA): $TELO"; exit 1; }
  klic POST /api/auth/prijava "{\"email\":\"test-$U-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
  ZETON="$(besedilo zeton)"
  [ -n "$ZETON" ] || { echo "Prijava ni uspela ($KODA): $TELO"; exit 1; }
  if [ "$U" = "a" ]; then ZETON_A="$ZETON"; else ZETON_B="$ZETON"; fi
done
echo "Priprava: uporabnika A in B registrirana in prijavljena"

# --- priprava: dve živili ---
zivilo() {
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/zivila" \
    -H "Content-Type: application/json" -H "X-Admin-Kljuc: $ADMIN_KLJUC" \
    --data "{\"ime\":\"$1 $CAS\",\"skupinaId\":$2,\"energijaKcal\":100}")
  TELO=$(cat "$TMP/telo")
  stevilka id
}
Z1="$(zivilo "Test moka" 3)"
Z2="$(zivilo "Test mleko" 5)"
[ -n "$Z1" ] && [ -n "$Z2" ] || { echo "Testnih živil ni bilo mogoče dodati: $TELO"; exit 1; }
echo "Priprava: testni živili $Z1 in $Z2"

SEST="[{\"ziviloId\":$Z1,\"kolicina\":250,\"enota\":\"g\"},{\"ziviloId\":$Z2,\"kolicina\":500,\"enota\":\"ml\"}]"
KOR='["1. Zmesaj moko in mleko.","2. Dodaj jajca in premesaj.","3. Peci na vroci ponvi."]'

echo
echo "1) Preveri z dodajanjem novega recepta"
klic POST /api/recepti "$(recept "Palacinke" 4 "$SEST" "$KOR")" "$ZETON_A"
preveri "POST /api/recepti doda recept" 201
RID="$(stevilka id)"
echo "    id novega recepta: $RID"

echo "2) Preveri z urejanjem že obstoječega recepta"
klic PUT "/api/recepti/$RID" "$(recept "Palacinke s cokolado" 6 "$SEST" "$KOR")" "$ZETON_A"
preveri "PUT /api/recepti/$RID uredi recept" 200
klic GET "/api/recepti/$RID" "" "$ZETON_A"
preveri_vsebuje "GET vrne novo ime" '"ime":"Palacinke s cokolado"'
preveri_vsebuje "GET vrne novo število porcij" '"steviloPorcij":6'

echo "3) Preveri z vnosom imena recepta"
klic POST /api/recepti "$(recept "" 4 "$SEST" "$KOR")" "$ZETON_A"
preveri "prazno ime je zavrnjeno" 400

echo "4) Preveri z dodajanjem sestavin in njihovih količin"
klic GET "/api/recepti/$RID" "" "$ZETON_A"
preveri_vsebuje "recept ima sestavino s količino 250 g" '"kolicina":250'
SEST_NIC="[{\"ziviloId\":$Z1,\"kolicina\":0,\"enota\":\"g\"}]"
klic POST /api/recepti "$(recept "Napacna kolicina" 4 "$SEST_NIC" "$KOR")" "$ZETON_A"
preveri "količina 0 je zavrnjena" 400
klic POST /api/recepti "$(recept "Neobstojece zivilo" 4 '[{"ziviloId":999999,"kolicina":100,"enota":"g"}]' "$KOR")" "$ZETON_A"
preveri "neobstoječe živilo je zavrnjeno" 400

echo "5) Preveri z dodajanjem korakov priprave"
klic GET "/api/recepti/$RID" "" "$ZETON_A"
preveri_vsebuje "koraki so v pravem vrstnem redu" "\"koraki\":$KOR"
klic POST /api/recepti "$(recept "Prazen korak" 4 "$SEST" '["Zmesaj",""]')" "$ZETON_A"
preveri "prazen korak je zavrnjen" 400

echo "6) Preveri z določitvijo števila porcij"
klic POST /api/recepti "$(recept "Nic porcij" 0 "$SEST" "$KOR")" "$ZETON_A"
preveri "0 porcij je zavrnjeno" 400
klic POST /api/recepti "$(recept "Prevec porcij" 51 "$SEST" "$KOR")" "$ZETON_A"
preveri "51 porcij je zavrnjeno" 400

echo "7) Preveri z nalaganjem slike recepta"
printf '\x89PNG\r\n\x1a\ntestna-slika' > "$TMP/slika.png"
nalozi "/api/recepti/$RID/slika" "$TMP/slika.png" image/png "$ZETON_A"
preveri "nalaganje .png slike" 200
POT="$(besedilo slika)"
echo "    pot slike: $POT"
klic GET "$POT"
preveri "slika je dostopna na $POT" 200
printf '%%PDF-1.4 test' > "$TMP/dokument.pdf"
nalozi "/api/recepti/$RID/slika" "$TMP/dokument.pdf" application/pdf "$ZETON_A"
preveri ".pdf datoteka je zavrnjena" 400
dd if=/dev/zero of="$TMP/velika.jpg" bs=1048576 count=6 2>/dev/null
nalozi "/api/recepti/$RID/slika" "$TMP/velika.jpg" image/jpeg "$ZETON_A"
preveri "slika večja od 5 MB je zavrnjena" 400

echo "8) Preveri z obveznimi polji pri shranjevanju recepta"
klic POST /api/recepti "$(recept "Brez sestavin" 4 "[]" "$KOR")" "$ZETON_A"
preveri "recept brez sestavin je zavrnjen" 400
klic POST /api/recepti "$(recept "Brez korakov" 4 "$SEST" "[]")" "$ZETON_A"
preveri "recept brez korakov je zavrnjen" 400

echo "9) Dodatno: dostop"
klic GET "/api/recepti/$RID" "" "$ZETON_B"
preveri "drug uporabnik ne vidi tujega recepta" 404
klic PUT "/api/recepti/$RID" "$(recept "Vdor" 4 "$SEST" "$KOR")" "$ZETON_B"
preveri "drug uporabnik ne more urejati tujega recepta" 404
klic GET /api/recepti
preveri "brez prijave ni dostopa" 401

rm -rf "$TMP"
echo
echo "== Rezultat: $USPELO uspešnih, $PADLO neuspešnih =="
[ "$PADLO" = "0" ]
