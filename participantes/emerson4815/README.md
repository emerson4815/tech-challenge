# Entrega — emerson4815

## 1. Resumo da entrega

Corrigi o módulo de Beneficiários e implementei as funcionalidades ausentes da API: consulta por id, atualização, exclusão lógica, paginação e filtros combináveis.

O cadastro passou a validar CPF, data de nascimento, nome e existência do plano. A unicidade do CPF é garantida também no banco de dados para evitar duplicidade em requisições concorrentes.

Na listagem implementei paginação estável, filtros por situação e plano e adicionei, como funcionalidade extra, busca parcial por nome.

No frontend Angular implementei a área de Beneficiários seguindo o padrão existente em Planos: modelos tipados, serviço isolado, listagem paginada, filtros, cadastro, edição, exclusão, estados de carregamento e exibição dos erros retornados pela API.

---

## 2. Decisões

### 2.1 Defeitos que encontrei no código base

**1. Criação de beneficiários fora do contrato da API**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
- **O que estava errado:** o endpoint recebia diretamente a entidade `Beneficiario`, validava o CPF apenas pelo tamanho, tratava CPF duplicado como `400 Bad Request` e retornava `200 OK` na criação. Também não havia uma camada de serviço para concentrar as regras de negócio.
- **Como percebi:** leitura da SPEC, dos testes públicos e comparação com o padrão existente no módulo de Planos.
- **Como corrigi:** criei contratos específicos para Beneficiários e o `BeneficiarioServico`, movendo para ele as validações e regras de negócio. O POST passou a retornar `201 Created` com header `Location`, `409` para CPF duplicado e `422` para plano inexistente, além de validar CPF completo, dados obrigatórios e data de nascimento.
- **O que quebraria em produção:** o cliente poderia enviar campos que deveriam ser controlados pelo servidor, CPFs inválidos poderiam ser cadastrados e os consumidores da API receberiam códigos HTTP incompatíveis com o contrato.

**2. Listagem fazia consultas desnecessárias de Plano**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
- **O que estava errado:** a listagem carregava cada `Plano` com `FindAsync` dentro de um `foreach`.
- **Como percebi:** leitura do código e da exigência da SPEC de manter constante a quantidade de consultas em relação aos itens da página.
- **Como corrigi:** removi o carregamento da entidade `Plano`. O contrato exige somente `plano_id`, que já existe em `Beneficiario`.
- **O que quebraria em produção:** planos ainda não rastreados poderiam provocar consultas adicionais conforme os dados retornados, aumentando latência e carga no banco.

**3. CPF não estava protegido contra concorrência**

- **Onde:** configuração de `Beneficiario` no `AppDbContext` e `BeneficiariosController`.
- **O que estava errado:** havia apenas uma consulta prévia verificando se o CPF já existia. O próprio controller mencionava que a garantia definitiva seria um índice único no banco, porém esse índice não existia na configuração da entidade.
- **Como percebi:** comparação entre o código do controller, a configuração do EF Core e a regra de concorrência da SPEC.
- **Como corrigi:** adicionei um índice único para CPF e uma migration. A verificação prévia continua existindo para retornar um erro amigável, mas a garantia definitiva fica no PostgreSQL.
- **O que quebraria em produção:** duas requisições simultâneas poderiam passar pela consulta de existência antes de qualquer uma persistir o registro, permitindo CPFs duplicados sem a restrição no banco.

### 2.2 Pontos em que a especificação não definiu o comportamento

**1. Ordenação da paginação**

- **O que a spec não define:** qual deve ser a ordenação padrão da listagem.
- **O que decidi:** ordenar por `NomeCompleto` e utilizar `Id` como segundo critério.
- **Por quê:** o nome é útil para quem consulta a tela e o `Id` torna a ordenação determinística quando existem nomes iguais.
- **O que eu consideraria se fosse decidir diferente:** ordenar por `DataCadastro DESC` caso o produto priorizasse registros recentes.

### 2.3 Inconsistências que percebi

**1. Tamanho padrão da página**

- **A spec diz:** quando `tamanho` não é informado, o valor padrão é 10.
- **O teste espera:** o teste público original esperava 20.
- **Segui:** a SPEC.
- **Por quê:** ela define explicitamente o contrato do endpoint. Ajustei o teste para esperar 10.

**2. Alteração de beneficiário inativo**

- **A spec diz:** um beneficiário `INATIVO` é um registro congelado e tentativa de alteração cadastral deve retornar `409`.
- **O teste espera:** o teste público original esperava `200` e validava a alteração do nome.
- **Segui:** a SPEC.
- **Por quê:** a regra é explícita. Ajustei o teste para esperar `409`.

**3. Status 422 no PUT**

- **A spec diz:** na descrição detalhada do PUT, um `plano_id` inexistente deve retornar `422`.
- **A tabela geral diz:** apresenta apenas `200`, `400`, `404` e `409`.
- **Segui:** a regra específica do endpoint.
- **Por quê:** ela descreve diretamente o comportamento para esse cenário.

### 2.4 Decisões técnicas

- Criei `PaginacaoResponse<T>` genérico para não acoplar o envelope paginado ao módulo de Beneficiários.
- Implementei exclusão lógica com `ExcluidoEm` e `HasQueryFilter`.
- Na validação de CPF utilizo `IgnoreQueryFilters()`, pois o CPF de um beneficiário excluído deve continuar ocupado.
- Adicionei o filtro opcional `nome`, não exigido pela SPEC. Ele utiliza busca parcial e case-insensitive e pode ser combinado com os demais filtros.
- Adicionei testes de integração para busca por nome, cobrindo registro existente, inexistente, excluído e busca parcial/case-insensitive.
- No frontend mantive componentes simples e sem dependências visuais adicionais, priorizando comportamento e tratamento de erros.
- A busca por nome no Angular utiliza debounce com RxJS para evitar uma chamada à API a cada tecla digitada.

### 2.5 O que ficou de fora

Não adicionei recursos visuais ou bibliotecas de componentes além do necessário para a interface funcionar. A SPEC deixa claro que acabamento visual não faz parte da avaliação, então priorizei comportamento, integração com a API e código que eu consiga explicar e alterar durante a entrevista.
Também mantive `GET /planos` sem paginação. A SPEC apresenta o módulo de Planos como funcional e como referência para a implementação de Beneficiários, portanto evitei alterar seu contrato sem necessidade para os requisitos do desafio. No frontend, a lista completa também é utilizada nos campos de seleção de plano.

---

## 3. Uso de IA

**Nível de uso:** moderado

### 3.1 Ferramentas

- **ChatGPT:** apoio na leitura da SPEC, discussão das regras, revisão de implementações, testes e debugging.
- **Visual Studio Code:** desenvolvimento e edição do backend e frontend.
- **.NET 10 SDK / .NET CLI / xUnit:** compilação, execução e testes do backend.
- **Docker / Docker Compose:** execução e validação do ambiente completo da aplicação.

### 3.2 Os 3 prompts que mais influenciaram o resultado

**Prompt 1**

```text
Tenho uma aplicação com API em .NET e frontend em Angular, executados via Docker Compose junto com PostgreSQL. A entrega não pode utilizar build: no docker-compose.yml; o avaliador deve conseguir executar a aplicação apenas baixando as imagens públicas.

Com base na arquitetura prevista pelo desafio, me passe os comandos CLI necessários para gerar as imagens da API e do frontend em linux/amd64 e linux/arm64 e publicá-las no meu Docker Hub. Meu usuário no Docker Hub é emerson4815 e quero utilizar a tag 1.0.0.

As imagens devem ser:

emerson4815/desafio-4tech-api:1.0.0
emerson4815/desafio-4tech-web:1.0.0

Considere que, depois de publicadas, o docker-compose.yml da entrega deve apenas referenciar essas imagens, sem realizar build do código.
```

- **O que aceitei:** aceitei a sugestão de utilizar docker buildx para gerar imagens compatíveis com linux/amd64 e linux/arm64 e publicá-las diretamente no Docker Hub com --push e claro, conforme fui subindo outras versões, adaptei as tags das imagens

Também segui a orientação de versionar as imagens com a tag 1.0.0 e utilizar exatamente essas referências no docker-compose.yml e no info.json.

- **O que descartei e por quê:** não adotei a sugestão inicial de utilizar outro namespace do Docker Hub (emerson2342), pois identifiquei que minha conta atual estava vinculada ao usuário emerson4815. Ajustei os comandos e mantive as imagens no namespace correto.

Também não considerei suficiente apenas publicar as imagens. Preferi validar a entrega executando o Compose a partir das imagens publicadas, simulando o processo que será realizado pelo avaliador.

**Prompt 2**

```text
Baseado nos outros testes de listagem, crie 4 novos testes para testar nossa feature de query por nome.
```

- **O que aceitei:** utilizei a estrutura sugerida para criar os testes da busca por nome, cobrindo beneficiário existente, inexistente, excluído logicamente e busca parcial/case-insensitive.

- **O que descartei e por quê:** ajustei os testes gerados para seguir os helpers e o padrão já existente no projeto. Depois, validei a implementação executando a suíte completa, com 29 testes aprovados.

**Prompt 3**

```text
Ajuste meu método de busca por nome para utilizar debounce, evitando chamar a API a cada tecla digitada.
```

- **O que aceitei:** utilizei Subject e operadores do RxJS (debounceTime e distinctUntilChanged) para controlar as alterações do campo de busca antes de consultar a API.

- **O que descartei e por quê:** não adicionei bibliotecas externas para debounce, pois o projeto já utiliza RxJS e os recursos necessários já estavam disponíveis.

### 3.3 O que fiz sem IA

As decisões de seguir a SPEC quando encontrei divergências com os testes públicos foram minhas, como o tamanho padrão de 10 itens na paginação e o bloqueio de alteração cadastral de beneficiários `INATIVO`.

Também defini a ordenação da listagem por nome, a inclusão do filtro adicional por nome e a forma de apresentação dos dados no frontend.

Durante os testes manuais identifiquei problemas de integração e estado, como o uso de `AsNoTracking()` em uma entidade que posteriormente precisava ser alterada e salva.

A integração entre frontend e backend, os testes manuais pelo Swagger/navegador, a revisão dos resultados da suíte e os ajustes finais após execução foram feitos por mim.

### 3.4 O que ainda não domino

Tenho pouca experiência prática com Docker e conteinerização.

Nesta entrega, utilizei Docker para gerar e publicar as imagens da API e da interface, além do Docker Compose para executar os serviços e suas dependências. Para realizar essa parte, consultei a documentação e utilizei o ChatGPT como apoio para entender os comandos, configurações e o processo de publicação das imagens.

Hoje entendo o fluxo utilizado nesta entrega e consigo explicar seu funcionamento, mas, para cenários mais avançados de Docker, ainda precisaria consultar documentação e materiais de apoio.

## 4. Perguntas de compreensão

### 4.1 Concorrência

No `BeneficiarioServico`, antes da criação, `GarantirCpfUnicoAsync` verifica se o CPF já existe. Essa consulta trata o fluxo comum, mas não garante unicidade em caso de concorrência, pois duas requisições podem realizá-la antes que qualquer uma tenha persistido o registro.

A garantia definitiva está no índice único de Beneficiario.Cpf, configurado no AppDbContext e criado no PostgreSQL por migration.

Se duas requisições tentarem persistir o mesmo CPF simultaneamente, uma delas será concluída e a outra receberá do PostgreSQL a violação de unicidade 23505. O `SalvarAsync` trata a `DbUpdateException` correspondente e a converte em `ConflitoException`, fazendo a API responder `409 Conflict`.

### 4.2 Um defeito que você corrigiu

Um defeito que corrigi estava na listagem original de beneficiários em `BeneficiariosController`.

O código buscava os beneficiários e depois percorria a lista chamando `FindAsync` para carregar o `Plano` de cada registro. O comentário afirmava que o cache do `DbContext` manteria a operação em uma única ida ao banco.

Isso não é garantido: quando um plano ainda não está sendo rastreado, `FindAsync` precisa consultar o banco. Assim, a quantidade de consultas poderia crescer conforme aparecessem planos distintos na página.

Além disso, a SPEC exige na resposta apenas `plano_id`, que já existe na entidade `Beneficiario`, então carregar o objeto `Plano` era desnecessário.

Removi esse `foreach` e passei a aplicar filtros, contagem, ordenação e paginação diretamente na consulta de beneficiários.

Em produção, a implementação original poderia aumentar a latência e a carga no banco à medida que a quantidade e diversidade dos registros crescessem.

### 4.3 O trecho mais complexo

O trecho mais complexo gerado com apoio de IA foi a validação de CPF dentro de `BeneficiarioServico`.

O método começa verificando se o valor possui exatamente 11 caracteres e se todos são dígitos numéricos. Em seguida rejeita sequências repetidas, como `11111111111`.

Depois os caracteres são convertidos para números para calcular os dois dígitos verificadores.

Para o primeiro dígito, os nove primeiros números são multiplicados por pesos decrescentes de 10 até 2. A soma é usada para calcular o resto da divisão por 11 e, a partir dele, o primeiro dígito esperado.

O método compara esse resultado com o décimo número recebido. Se não for igual, o CPF é rejeitado.

O mesmo processo é repetido para o segundo dígito, agora considerando dez números e pesos de 11 até 2.

O CPF só é considerado válido quando os dois dígitos calculados correspondem aos dígitos recebidos. Essa validação trata a validade matemática; a unicidade é garantida separadamente pelo índice único no banco.
