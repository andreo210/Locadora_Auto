---
name: commit-seguro
description: Faz commit neste repositório evitando a armadilha de fim de linha (CRLF→LF) que faz o git reportar ~350 arquivos modificados. Mostra o diff real, adiciona arquivos explicitamente e comita. Use sempre que for comitar no Locadora_Auto.
disable-model-invocation: true
---

# Commit seguro

Este repositório é editado pelo WSL e não tem `.gitattributes`, então `git status` reporta centenas de arquivos modificados que só mudaram de CRLF para LF. Um `git add -A` comita uma reescrita de fim de linha em todo o repositório e destrói o histórico útil.

Argumento opcional: mensagem de commit em `$ARGUMENTS`.

## Passos

1. **Mostre a mudança real**, ignorando espaço em branco e fim de linha:
   ```bash
   git diff --ignore-all-space --stat
   git diff --cached --ignore-all-space --stat
   ```
   Se isso vier vazio mas `git status` estiver cheio, não há nada a comitar — avise o usuário e pare.

2. **Inspecione o conteúdo** dos arquivos que aparecerem, com `git diff --ignore-all-space -- <arquivo>`, para escrever uma mensagem honesta.

3. **Adicione explicitamente**, um caminho por vez:
   ```bash
   git add <caminho/exato/do/arquivo>
   ```
   Nunca `git add -A`, `git add .`, `git add --all` nem `git commit -a`. Um hook do projeto bloqueia essas formas; se você for bloqueado, é esse o motivo — adicione os caminhos individualmente.

   Arquivos novos (untracked) precisam ser confirmados com o usuário antes de entrar — o repositório tem imagens soltas em `Locadora_Auto.Front/wwwroot/img/` que podem não ser intencionais.

4. **Confira o que foi realmente preparado** antes de comitar:
   ```bash
   git diff --cached --stat
   ```
   Se aparecerem arquivos que você não adicionou de propósito, remova-os com `git restore --staged <arquivo>` e investigue.

5. **Comite.** Mensagem curta, minúscula, em português, sem prefixo de conventional commits — é o padrão do histórico (`listar cliente services`, `criar funcionario`, `tabela generica`).

Não faça `push` a menos que o usuário peça. O trabalho acontece em `andre-dev` e vai para `main` por PR.
