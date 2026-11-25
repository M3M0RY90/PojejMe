#!/usr/bin/env bash
# Avtomatsko preverjanje pogojev zadovoljstva UZ-3.3 Samodejni izračun kalorij in makrohranil (#26)
#
# Uporaba:
#   1. docker compose up -d          (baza)
#   2. cd PojejMe.Api && dotnet run  (API, privzeto http://localhost:5165)
#   3. v drugem terminalu: bash testi/uz-3.3-samodejni-izracun.sh [URL]
#
# Hranilne vrednosti morajo biti v odgovoru z receptom že ob dodajanju in urejanju (brez dodatnega klica).
# Testna živila (na 100 g):  moka 364/10/76/1   mleko 64/3.3/4.8/3.6   maslo 717/0.9/0.1/81
#
# Recept: 200 g moke + 300 ml mleka + 50 g masla, 2 porciji
#   skupaj:     1278.5 kcal, 30.4 g B, 166.5 g OH, 53.3 g M
#   na porcijo:  639.3 kcal, 15.2 g B,  83.2 g OH, 26.7 g M
#   prispevki:  moka 728 kcal, mleko 192 kcal, maslo 358.5 kcal
# Po spremembi masla na 100 g:
#   skupaj:     1637 kcal, 30.8 g B, 166.5 g OH, 93.8 g M ; na porcijo 818.5 kcal, 46.9 g M
# Po spremembi na 4 porcije: na porcijo 409.3 kcal (skupaj ostane 1637)

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
  del=$(echo "$TELO" | grep -o "\"$1\":{[^}]*}" | head -1)
  if echo "$del" | grep -qF "$3"; then
    echo "  ✔ $2"; USPELO=$((USPELO + 1))
  else
    echo "  ✘ $2 (v \"$1\" ni $3)"
    echo "    $1: $del"
    PADLO=$((PADLO + 1))
  fi
}

# preveri_prispevek IME_ZIVILA OPIS BESEDILO  -> prispevek posamezne sestavine
preveri_prispevek() {
  local del
  del=$(echo "$TELO" | grep -o "\"zivilo\":\"$1\",\"kolicina\":[0-9.]*,\"enota\":\"[a-z]*\",\"hranila\":{[^}]*}" | head -1)
  if echo "$del" | grep -qF "$3"; then
    echo "  ✔ $2"; USPELO=$((USPELO + 1))
  else
    echo "  ✘ $2 (v prispevku ni $3)"
    echo "    prispevek: $del"
    PADLO=$((PADLO + 1))
  fi
}

stevilka() { echo "$TELO" | grep -o "\"$1\":[0-9]*" | head -1 | sed 's/.*://'; }
besedilo() { echo "$TELO" | grep -o "\"$1\":\"[^\"]*\"" | head -1 | sed 's/^[^:]*:"//; s/"$//'; }

echo "== UZ-3.3 Samodejni izračun kalorij in makrohranil — testiranje na $URL =="

klic GET /api/status
if [ "$KODA" != "200" ]; then
  echo "API ne teče na $URL. Najprej zaženi: docker compose up -d  in  cd PojejMe.Api && dotnet run"
  exit 1
fi

klic POST /api/auth/registracija "{\"ime\":\"Tester 33\",\"email\":\"si-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
klic POST /api/auth/prijava "{\"email\":\"si-$CAS@pojej.me\",\"geslo\":\"TestGeslo123!\"}"
ZETON="$(besedilo zeton)"
[ -n "$ZETON" ] || { echo "Prijava ni uspela ($KODA): $TELO"; exit 1; }

zivilo() { # IME SKUPINA KCAL B OH M
  KODA=$(curl -s -o "$TMP/telo" -w "%{http_code}" -X POST "$URL/api/zivila" \
    -H "Content-Type: application/json" -H "X-Admin-Kljuc: $ADMIN_KLJUC" \
    --data "{\"ime\":\"$1\",\"skupinaId\":$2,\"energijaKcal\":$3,\"beljakovine\":$4,\"ogljikoviHidrati\":$5,\"mascobe\":$6}")
  TELO=$(cat "$TMP/telo")
  stevilka id
}
I_MOKA="SI moka $CAS"; I_MLEKO="SI mleko $CAS"; I_MASLO="SI maslo $CAS"
MOKA="$(zivilo "$I_MOKA" 3 364 10 76 1)"
MLEKO="$(zivilo "$I_MLEKO" 5 64 3.3 4.8 3.6)"
MASLO="$(zivilo "$I_MASLO" 10 717 0.9 0.1 81)"
[ -n "$MOKA" ] && [ -n "$MLEKO" ] && [ -n "$MASLO" ] || { echo "Testnih živil ni bilo mogoče dodati: $TELO"; exit 1; }
echo "Priprava: uporabnik, živila moka=$MOKA mleko=$MLEKO maslo=$MASLO"

KOR='["Zmesaj.","Speci."]'
sestavine() { # KOLICINA_MASLA
  echo "[{\"ziviloId\":$MOKA,\"kolicina\":200,\"enota\":\"g\"},{\"ziviloId\":$MLEKO,\"kolicina\":300,\"enota\":\"ml\"},{\"ziviloId\":$MASLO,\"kolicina\":$1,\"enota\":\"g\"}]"
}
recept() { # KOLICINA_MASLA PORCIJE
  echo "{\"ime\":\"SI pecivo\",\"steviloPorcij\":$2,\"sestavine\":$(sestavine "$1"),\"koraki\":$KOR}"
}

echo
echo "1) Preveri z receptom, ki vsebuje različne sestavine (moka, mleko, maslo)"
klic POST /api/recepti "$(recept 50 2)" "$ZETON"
preveri "POST /api/recepti" 201
RID="$(stevilka id)"
if echo "$TELO" | grep -q '"hranilneVrednosti":{'; then
  echo "  ✔ hranilne vrednosti so že v odgovoru ob dodajanju (brez dodatnega klica)"; USPELO=$((USPELO + 1))
else
  echo "  ✘ v odgovoru ni hranilneVrednosti"; PADLO=$((PADLO + 1))
fi
preveri_prispevek "$I_MOKA" "prispevek moke (200 g) = 728 kcal" '"energijaKcal":728'
preveri_prispevek "$I_MLEKO" "prispevek mleka (300 ml) = 192 kcal" '"energijaKcal":192'
preveri_prispevek "$I_MASLO" "prispevek masla (50 g) = 358.5 kcal" '"energijaKcal":358.5'

echo "2) Preveri z izračunom skupnega števila kalorij"
preveri_v skupaj "kalorije skupaj = 1278.5 kcal" '"energijaKcal":1278.5'
echo "3) Preveri z izračunom beljakovin"
preveri_v skupaj "beljakovine skupaj = 30.4 g" '"beljakovine":30.4'
echo "4) Preveri z izračunom ogljikovih hidratov"
preveri_v skupaj "ogljikovi hidrati skupaj = 166.5 g" '"ogljikoviHidrati":166.5'
echo "5) Preveri z izračunom maščob"
preveri_v skupaj "maščobe skupaj = 53.3 g" '"mascobe":53.3'

echo "6) Preveri z izračunom hranilnih vrednosti glede na število porcij (2 porciji)"
preveri_v naPorcijo "kalorije na porcijo = 639.3 kcal" '"energijaKcal":639.3'
preveri_v naPorcijo "beljakovine na porcijo = 15.2 g" '"beljakovine":15.2'
preveri_v naPorcijo "ogljikovi hidrati na porcijo = 83.2 g" '"ogljikoviHidrati":83.2'
preveri_v naPorcijo "maščobe na porcijo = 26.7 g" '"mascobe":26.7'

echo "7) Preveri s spremembo količine sestavine in ponovnim izračunom (maslo 50 g -> 100 g)"
klic PUT "/api/recepti/$RID" "$(recept 100 2)" "$ZETON"
preveri "PUT /api/recepti/$RID" 200
preveri_prispevek "$I_MASLO" "nov prispevek masla (100 g) = 717 kcal" '"energijaKcal":717'
preveri_v skupaj "nove kalorije skupaj = 1637 kcal" '"energijaKcal":1637'
preveri_v skupaj "nove maščobe skupaj = 93.8 g" '"mascobe":93.8'
preveri_v naPorcijo "nove kalorije na porcijo = 818.5 kcal" '"energijaKcal":818.5'
klic GET "/api/recepti/$RID" "" "$ZETON"
preveri_v skupaj "GET recepta vrne nov izračun (1637 kcal)" '"energijaKcal":1637'
klic GET /api/recepti "" "$ZETON"
preveri_v skupaj "tudi seznam receptov vsebuje izračun (1637 kcal)" '"energijaKcal":1637'

echo "   ... in sprememba števila porcij (2 -> 4)"
klic PUT "/api/recepti/$RID" "$(recept 100 4)" "$ZETON"
preveri_v skupaj "skupaj ostane 1637 kcal" '"energijaKcal":1637'
preveri_v naPorcijo "na porcijo = 409.3 kcal" '"energijaKcal":409.3'

echo "8) Dodatno: usklajenost z UZ-3.2"
klic GET "/api/recepti/$RID/hranilna-vrednost" "" "$ZETON"
preveri_v skupaj "GET /hranilna-vrednost vrne enak rezultat (1637 kcal)" '"energijaKcal":1637'

rm -rf "$TMP"
echo
echo "== Rezultat: $USPELO uspešnih, $PADLO neuspešnih =="
[ "$PADLO" = "0" ]
