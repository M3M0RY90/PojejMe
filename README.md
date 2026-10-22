# Pojej.me

Pametna shramba za zdravo prehrano brez zavržkov.

## Docker

```bash
docker build -t pojejme-api:latest .
docker run -p 8000:8080 --name pojejme-api -d pojejme-api:latest
```

Ob vsakem pushu v `main` GitHub Actions zgradi sliko in jo objavi na Docker Hubu kot
`memory1966/pojejme-api` z oznakama `latest` in SHA commita.
