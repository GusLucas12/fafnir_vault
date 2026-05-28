# Deploy do backend

## Subir com Docker neste servidor

1. Copie `.env.example` para `.env`
2. Ajuste `FAFNIR_CONNECTION_STRING`, `AUTH_TOKEN_SECRET` e `CORS_ALLOWED_ORIGIN_1`
3. Rode:

```bash
docker compose up -d --build
```

## Atualizar automaticamente no push da `main`

O repositório agora inclui um workflow em `.github/workflows/deploy-main.yml` que faz deploy por SSH sempre que houver `push` na branch `main`.

Fluxo:

1. A action conecta no servidor via SSH
2. Entra no diretório definido em `DEPLOY_PATH`
3. Executa `bash ./scripts/deploy-on-server.sh`
4. O script faz `git pull --ff-only origin main`
5. Recria o container com `docker compose up -d --build --remove-orphans`

Segredos que voce precisa cadastrar no GitHub (`Settings > Secrets and variables > Actions`):

- `DEPLOY_HOST`: IP ou dominio do servidor
- `DEPLOY_USER`: usuario SSH
- `DEPLOY_SSH_KEY`: chave privada usada pela action
- `DEPLOY_PATH`: pasta do clone de deploy no servidor, por exemplo `/home/ubuntu/fafnir_vault`
- `DEPLOY_PORT`: opcional, padrao `22`

Recomendacao importante:

- Use um clone dedicado para deploy no servidor, limpo, sem alteracoes locais
- O script usa `git pull --ff-only`; se houver mudancas locais nesse clone, o deploy vai falhar para evitar sobrescrever trabalho
- Garanta que Docker e Docker Compose Plugin estejam instalados no servidor
- Garanta que o arquivo `.env` exista dentro de `DEPLOY_PATH`

Defaults de seguranca:

- `Swagger` desligado em producao
- `CORS` liberado apenas para as origins configuradas
- Redirecionamento HTTPS controlado por `ENABLE_HTTPS_REDIRECTION`

## Publicar direto do Windows

No PowerShell, dentro da pasta do repositório:

```powershell
Set-ExecutionPolicy -Scope Process Bypass

.\publish-windows.ps1 `
  -ImageName "fafnir-vault-backend" `
  -Tag "v1.0.0"
```

Para enviar a imagem para um registry:

```powershell
.\publish-windows.ps1 `
  -Registry "ghcr.io/seu-usuario" `
  -ImageName "fafnir-vault-backend" `
  -Tag "v1.0.0" `
  -Push
```
