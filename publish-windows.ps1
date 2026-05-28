param(
    [string]$ImageName = "fafnir-vault-backend",
    [string]$Tag = "latest",
    [string]$Registry,
    [switch]$Push
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Comando obrigatorio nao encontrado: $Name"
    }
}

Require-Command "docker"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$fullImageName = if ([string]::IsNullOrWhiteSpace($Registry)) {
    "${ImageName}:${Tag}"
} else {
    "$($Registry.TrimEnd('/'))/${ImageName}:${Tag}"
}

Write-Host "Gerando imagem Docker $fullImageName"

docker build `
    --file (Join-Path $scriptDirectory "Dockerfile") `
    --tag $fullImageName `
    $scriptDirectory

if ($Push) {
    Write-Host "Enviando imagem para o registry"
    docker push $fullImageName
}

Write-Host ""
Write-Host "Concluido."
Write-Host "Imagem: $fullImageName"
