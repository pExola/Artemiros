using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using Unity.Properties;
using UnityEditor.Tilemaps;

/// <summary>
/// Classe principal que controla toda a lógica do jogo.
/// É responsável por carregar os níveis, gerenciar o grid, o tempo, a bandeja,
/// o input do jogador e as condições de vitória/derrota.
/// </summary>
public class GridController : MonoBehaviour
{
    // --- VARIÁVEIS DE CONFIGURAÇÃO (DEFINIDAS NO INSPECTOR) ---

    [Header("Configuração do Nível")]
    [Tooltip("Arraste aqui o asset LevelGroup que contém os estágios do nível.")]
    public LevelGroup levelGroupAtual;
    public float tempoInicialDoNivel = 60f;

    [Header("Referências de UI")]
    public RelogioUIController relogioVisual;
    public RectTransform boardContainer;
    public Transform armazemUIParent;
    public GameObject painelVitoria;
    public GameObject painelDerrota;

    [Header("Configuração da Animação")]
    [Tooltip("Duração da animação da peça caindo na bandeja.")]
    public float duracaoAnimacao = 0.3f;
    [Tooltip("Pausa em segundos após uma combinação ser feita, antes das peças sumirem.")]
    public float delayAposCombinacao = 0.25f;
    [Tooltip("Ajuste fino para a posição X das peças Bis horizontais.")]
    public float deslocamentoHorizontalBis = -10f;

    [Header("Prefabs")]
    public GameObject slotPrefab;
    public GameObject slotBisPrefab;
    public GameObject monstroIconPrefab;
    public GameObject iconeBandejaNormalPrefab;
    public GameObject iconeBandejaBisPrefab;

    [Header("Sprites")]

    [Tooltip("Sprite da caixa que é revelada quando um monstro é removido.")]
    public Sprite spriteCaixa;
    public Sprite spriteParede;

    [Tooltip("CARALHO, ISSO É A SPRITE QUE FICA NA BANDEJA!!!!!!")]
    public List<Sprite> monstroSpritesBandeira;

    [Tooltip("Lista de sprites para as peças em estado DESBLOQUEADO.")]
    public List<Sprite> spritesDesbloqueados; 
    public List<Sprite> spritesDesbloqueadosBis; 
    public List<Sprite> spritesDesbloqueadosTris; 

    [Tooltip("Lista de sprites para as peças em estado BLOQUEADO.")]
    public List<Sprite> spritesBloqueados;
    public List<Sprite> spritesBloqueadosBis;
    public List<Sprite> spritesBloqueadosTris;

    [Header("Referências de Sistema")]
    public GraphicRaycaster graphicRaycaster;
    public EventSystem eventSystem;
    public Canvas rootCanvas;

    [Header("Debug")]
    [Tooltip("Qual estágio do LevelGroup deve ser desenhado na Scene? (0, 1, 2, etc.)")]
    [Range(0, 10)]
    public int estagioParaVisualizar = 0;

    // --- VARIÁVEIS DE ESTADO INTERNO DO JOGO ---
    private List<List<Monstro>> Monstros = new List<List<Monstro>>(); // Matriz de dados que representa o tabuleiro lógico
    public List<Monstro> Armazem = new List<Monstro>(); // Lista de dados da bandeja secundária
    private int gridWidth, gridHeight, maxArmazem;
    private float tempoRestante;
    private int estagioAtual = 0;
    private bool jogoTerminou = false;
    private bool isAnimating = false; // Trava o input do jogador durante as animações

    // --- MÉTODOS DO CICLO DE VIDA DO UNITY ---

    /// <summary>
    /// Chamado uma vez quando o script é iniciado.
    /// Prepara o estado inicial do jogo.
    /// </summary>

    void Start()
    {
        if (painelVitoria != null) painelVitoria.SetActive(false);
        if (painelDerrota != null) painelDerrota.SetActive(false);

        tempoRestante = tempoInicialDoNivel;
        StartCoroutine(InicializarEstagioAposLayout(estagioAtual));
    }

    /// <summary>
    /// Chamado a cada frame.
    /// Responsável por atualizar o timer e detectar o clique do jogador.
    /// </summary>

    void Update()
    {
        // Se o jogo acabou (vitória/derrota), congela toda a lógica.
        if (jogoTerminou) return;

        // Atualiza o contador de tempo e verifica a condição de derrota por tempo.
        if (tempoRestante > 0)
        {
            tempoRestante -= Time.deltaTime;
            if (relogioVisual != null)
                relogioVisual.AtualizarDisplay(tempoRestante, tempoInicialDoNivel);
        }
        else
        {
            Derrota("Tempo Esgotado!");
            return;
        }

        // Detecta o clique do mouse, mas somente se não houver uma animação em andamento.
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerEventData, results);

            if (results.Count > 0)
            {
                Monstro monstroClicado = results[0].gameObject.GetComponentInParent<Monstro>();
                if (monstroClicado != null)
                    ProcessarCliqueNoMonstro(monstroClicado);
            }
        }
    }

    // --- LÓGICA DE INICIALIZAÇÃO DO JOGO ---

    /// <summary>
    /// Corrotina que espera o final do frame para garantir que a UI
    /// tenha seu tamanho final calculado antes de construirmos o grid.
    /// </summary>
    IEnumerator InicializarEstagioAposLayout(int indiceEstagio)
    {
        yield return new WaitForEndOfFrame();
        IniciarEstagio(indiceEstagio);
    }

    /// <summary>
    /// Constrói o tabuleiro visual e lógico para um determinado estágio.
    /// </summary>

    void IniciarEstagio(int indiceEstagio)
    {
        if (levelGroupAtual == null || levelGroupAtual.estagios.Count <= indiceEstagio)
        {
            Derrota("Erro de configuração de nível.");
            return;
        }

        LevelData nivelData = levelGroupAtual.estagios[indiceEstagio];
        Debug.Log($"Iniciando Estágio {indiceEstagio + 1}: {nivelData.name}");
        foreach (var bis in nivelData.BisCoords)
            Debug.Log($"bis {bis.x1},{bis.y1} - {bis.x2},{bis.y2}");

        Armazem.Clear();
        AtualizarUIArmazem();

        foreach (Transform child in boardContainer)
            Destroy(child.gameObject);

        gridHeight = nivelData.layoutDoGrid.Count;
        gridWidth = (gridHeight > 0) ? nivelData.layoutDoGrid[0].colunas.Count : 0;
        maxArmazem = nivelData.TamanhoInventario;

        GridLayoutGroup gridLayout = boardContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = gridWidth;
        }

        Monstros = new List<List<Monstro>>();
        for (int i = 0; i < gridWidth; i++)
            Monstros.Add(new List<Monstro>(new Monstro[gridHeight]));

        HashSet<Tuple<int, int>> bisCoordinates = new HashSet<Tuple<int, int>>();
        foreach (var bisData in nivelData.BisCoords)
        {
            bisCoordinates.Add(new Tuple<int, int>(bisData.x1, bisData.y1));
            bisCoordinates.Add(new Tuple<int, int>(bisData.x2, bisData.y2));
        }

        // Itera por cada coordenada para criar os "Slots" (célula + peça).
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // Mapeia a coordenada Y do loop para o índice da lista no LevelData.
                // "y = 0" no código corresponde ao "Element 0" na lista do Inspector (linha de baixo).
                int linhaDoLayout = y;
                LevelData.TileConfig tile = nivelData.layoutDoGrid[linhaDoLayout].colunas[x];

                bool isBis = bisCoordinates.Contains(new Tuple<int, int>(x, y));
                GameObject prefabParaInstanciar = isBis ? slotBisPrefab : slotPrefab;
                // 1. Instancia o "Prefab Combo"
                GameObject slotObj = Instantiate(prefabParaInstanciar, boardContainer);
                slotObj.name = $"Slot ({x},{y})" + (isBis ? " - BIS" : "");

                // 2. Encontra as partes dentro do prefab instanciado (use os nomes exatos que você deu)
                Transform pecaTransform = slotObj.transform.Find("Peca_Monstro");
                if (pecaTransform == null)
                {
                    Debug.LogError("Objeto 'Peca_Monstro' não encontrado dentro do Slot_Prefab!", slotObj);
                    continue;
                }

                //Image pecaImage = pecaTransform.GetComponent<Image>();
                Monstro monstroComponent = pecaTransform.GetComponent<Monstro>();

                // 3. Configura o slot com base nos dados do nível
                if (tile.tipo == LevelData.TipoDeTile.Vazio)
                {
                    pecaTransform.gameObject.SetActive(false); // Desativa só a peça
                    Monstros[x][y] = null; // Marca a posição como vazia
                }
                else
                {

                    // Configura os dados do monstro com base no ScriptableObject
                    monstroComponent.posicaoGrid = new Tuple<int, int>(x, y);
                    monstroComponent.escondido = tile.escondido;
                    monstroComponent.cor = (tile.tipo == LevelData.TipoDeTile.Parede) ? -1 : tile.corDoMonstro;
                    monstroComponent.gridController = this;

                    if (tile.tipo == LevelData.TipoDeTile.Gerador && pecaTransform.TryGetComponent(out GeradorDeMonstros geradorComponent))
                    {
                        geradorComponent.monstrosParaGerar = tile.MonstrosASeremGeradosPeloGerador;
                    }
                    else if (tile.escondido)
                    {

                    }

                    Monstros[x][y] = monstroComponent;
                }
            }
        }
        // Agora vou acessar todos os monstros e adicionar os bis e tris nessa paradinha aqui

        for (int i = 0; i < nivelData.BisCoords.Count; i++)
        {
            LevelData.BisData monstro1 = nivelData.BisCoords[i];

            Monstro a, b;
            a = Monstros[monstro1.x1][monstro1.y1];
            b = Monstros[monstro1.x2][monstro1.y2];

            a.segundaParte = b;
            b.segundaParte = a;
        }

        for(int i = 0; i < nivelData.TrisCoords.Count; i++)
        {
            LevelData.TrisData monstro1 = nivelData.TrisCoords[i];

            Monstro a, b,c;
            a = Monstros[monstro1.x1][monstro1.y1];
            b = Monstros[monstro1.x2][monstro1.y2];
            c = Monstros[monstro1.x3][monstro1.y3];
            a.segundaParte = b;
            a.terceiraParte = c;

            b.segundaParte = a;
            b.terceiraParte = c;

            c.segundaParte = a;
            c.terceiraParte = b;
        }

        float cellWidth = gridLayout.cellSize.x;
        float cellHeight = gridLayout.cellSize.y;
        float spacingX = gridLayout.spacing.x;
        float spacingY = gridLayout.spacing.y;

        // Etapa 3: Percorre a lista de Bis novamente para ajustar o visual.
        foreach (var bisData in nivelData.BisCoords)
        {
            Monstro monstroA = Monstros[bisData.x1][bisData.y1];
            Monstro monstroB = Monstros[bisData.x2][bisData.y2];

            if (monstroA == null || monstroB == null) continue;

            // Define qual peça será a principal (visível) e qual será a secundária (invisível).
            // A principal será sempre a da Esquerda (se horizontal) ou a de Baixo (se vertical).
            bool isHorizontal = monstroA.posicaoGrid.Item2 == monstroB.posicaoGrid.Item2;
            Monstro principal = (isHorizontal && monstroA.posicaoGrid.Item1 < monstroB.posicaoGrid.Item1) || (!isHorizontal && monstroA.posicaoGrid.Item2 < monstroB.posicaoGrid.Item2) ? monstroA : monstroB;
            Monstro secundario = (principal == monstroA) ? monstroB : monstroA;

            // Esconde a imagem da peça secundária.
            Image imgSecundaria = secundario.GetComponent<Image>();
            if (imgSecundaria != null)
            {
                imgSecundaria.enabled = false;
            }

            // Pega o RectTransform da peça principal para manipulação.
            RectTransform rtPrincipal = principal.GetComponent<RectTransform>();
            if (isHorizontal)
            {
                rtPrincipal.anchoredPosition = new Vector2((cellWidth / 2 + spacingX / 2) + deslocamentoHorizontalBis, 0);
            }
            else // Vertical
            {
                rtPrincipal.anchoredPosition = new Vector2(0, cellHeight / 2 + spacingY / 2);
            }
            
            if (isHorizontal)
            {
                rtPrincipal.sizeDelta = new Vector2((cellWidth * 2) + spacingX, cellHeight);
            }
            else 
            {
                rtPrincipal.sizeDelta = new Vector2(cellWidth, (cellHeight * 2) + spacingY);
            }

        }

        // Após construir o grid, atualiza o visual de todas as peças (bloqueado/desbloqueado).
        AtualizarVisualsDoGrid();
    }

    // --- LÓGICA DE JOGABILIDADE E ANIMAÇÃO ---

    /// <summary>
    /// Método chamado quando um clique válido em uma peça é detectado.
    /// </summary>

    public void ProcessarCliqueNoMonstro(Monstro monstroClicado)
    {
        // Ignora cliques em peças já escondidas, vazias ou paredes.
        if (monstroClicado == null || !monstroClicado.gameObject.activeInHierarchy || monstroClicado.vazio || monstroClicado.cor == -1) return;

        // Verifica a regra principal: a peça pode ser removida e há espaço na bandeja?
        if (PodeRemover(monstroClicado) && Armazem.Count < maxArmazem)
        {
            // O clique agora inicia a Corrotina de JOGADA, não apenas de animação
            StartCoroutine(ExecutarJogada(monstroClicado));
        }
    }

    /// <summary>
    /// Função que retorna o tipo de monstro 
    /// </summary>

    public int PegarTipoDeMonstro(Monstro monstro)
    {
        if (monstro.terceiraParte is not null)
            return 3;
        else if (monstro.segundaParte is not null)
            return 2;
        else
            return 1;
    }

    /// <summary>
    /// Corrotina que gerencia toda a sequência de uma jogada: lógica, animação e checagem de resultados.
    /// </summary>

    IEnumerator ExecutarJogada(Monstro monstro)
    {

        isAnimating = true;

        // 1. Guarda a posição inicial e o sprite para a animação.
        Vector3 startPosition = monstro.transform.position;
        Sprite spriteDaPeca = monstro.GetComponent<Image>().sprite;

        // 2. Atualiza a lógica do jogo (os dados).
        AdicionarAoArmazem(monstro);
        RemoverDoGrid(monstro);
        AtualizarVisualsDoGrid();
        // 3. ATUALIZA A UI DA BANDEJA IMEDIATAMENTE.
        // Isso cria todos os ícones, incluindo o novo.
        AtualizarUIArmazem();
        yield return new WaitForEndOfFrame();
        // 4. Encontra o ícone que acabamos de adicionar e o esconde temporariamente.
        int indiceFinal = Armazem.IndexOf(monstro);
        if (indiceFinal < 0 || indiceFinal >= armazemUIParent.childCount)
        {
            // Se algo deu errado e não encontramos o monstro, cancela a animação para evitar erros.
            Debug.LogError("Não foi possível encontrar o monstro recém-adicionado na bandeja. Verifique a lógica de AdicionarAoArmazem.");
            isAnimating = false;
            yield break;
        }

        Transform iconeFinal = armazemUIParent.GetChild(indiceFinal);
        Image imagemDoIconeFinal = iconeFinal.GetComponent<Image>();

        if (imagemDoIconeFinal != null)
        {
            imagemDoIconeFinal.enabled = false; // Esconde o ícone para a animação acontecer.
        }

        // 5. Inicia a animação, que agora sabe a posição final correta.
        yield return StartCoroutine(AnimarPecaParaBandeja(startPosition, spriteDaPeca, iconeFinal));

        // 6. Lógica pós-jogada.
        bool combinacaoFeita = Armazem.GroupBy(m => m.cor).Any(g => g.Sum(m => PegarTipoDeMonstro(m)) >= 3);
        if (combinacaoFeita)
        {
            yield return new WaitForSeconds(delayAposCombinacao);
        }

        ChecarEEliminarGrupos();
        VerificarVitoriaDoEstagio();

        isAnimating = false;
    }

    /// <summary>
    /// Corrotina para animar os ícones da bandeja durante a reorganização ("esteira").
    /// </summary>

    IEnumerator AnimarIconeDaBandeja(Transform icone, Vector3 targetPosition)
    {
        Vector3 startPosition = icone.position;
        float elapsedTime = 0f;
        float duration = duracaoAnimacao / 2; // Animação mais rápida para a esteira

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            icone.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            yield return null;
        }
        // Garante a posição final
        icone.position = targetPosition;
    }

    /// <summary>
    /// Corrotina que anima uma peça "fantasma" da sua posição no grid até a bandeja.
    /// </summary>

    IEnumerator AnimarPecaParaBandeja(Vector3 startPosition, Sprite spriteDaPeca, Transform iconeDeDestino)
    {
        GameObject pecaFantasma = new GameObject("PecaAnimada");
        Image imgFantasma = pecaFantasma.AddComponent<Image>();
        imgFantasma.sprite = spriteDaPeca;

        RectTransform rtFantasma = pecaFantasma.GetComponent<RectTransform>();
        rtFantasma.SetParent(rootCanvas.transform, true);

        // Adiciona uma verificação aqui também, por segurança.
        if (iconeDeDestino != null)
        {
            rtFantasma.sizeDelta = iconeDeDestino.GetComponent<RectTransform>().sizeDelta;
        }
        else
        {
            // Se o ícone já foi destruído, usa um tamanho padrão.
            rtFantasma.sizeDelta = new Vector2(70, 70);
        }

        rtFantasma.position = startPosition;

        // A posição final precisa ser lida antes que o objeto seja destruído.
        Vector3 endPosition = (iconeDeDestino != null) ? iconeDeDestino.position : rtFantasma.position;

        float elapsedTime = 0f;
        while (elapsedTime < duracaoAnimacao)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duracaoAnimacao;
            rtFantasma.position = Vector3.Lerp(startPosition, endPosition, t);
            rtFantasma.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, t);
            yield return null;
        }

        Destroy(pecaFantasma);

        // --- A CORREÇÃO PRINCIPAL ESTÁ AQUI ---
        // Antes de tentar acessar o componente, verifica se o objeto ainda existe.
        if (iconeDeDestino != null)
        {
            Image imagemDoIconeFinal = iconeDeDestino.GetComponent<Image>();
            if (imagemDoIconeFinal != null)
            {
                imagemDoIconeFinal.enabled = true;
            }
        }
    }



    // --- MÉTODOS DE MANIPULAÇÃO DE DADOS E ESTADO ---

    /// <summary>
    /// Adiciona a referência de um Monstro à lista de dados da bandeja e a reordena.
    /// </summary>

    void AdicionarAoArmazem(Monstro monstro)
    {
        if (!Armazem.Contains(monstro))
        {
            Armazem.Add(monstro);
        }
        Armazem.Sort((a, b) => a.cor.CompareTo(b.cor));
    }

    /// <summary>
    /// Remove uma peça da lógica do grid, escondendo sua imagem.
    /// </summary>

    void RemoverDoGrid(Monstro monstro)
    {
        int tipoMonstro = PegarTipoDeMonstro(monstro);
        if ( tipoMonstro == 2)
        {
            Monstro secundario = monstro.segundaParte;
            Monstros[secundario.posicaoGrid.Item1][secundario.posicaoGrid.Item2] = null;
            Image img_secundario = secundario.GetComponent<Image>();
            if (img_secundario != null)
            {
                img_secundario.enabled = false;
            }
        }
        Monstros[monstro.posicaoGrid.Item1][monstro.posicaoGrid.Item2] = null;

        Image img = monstro.GetComponent<Image>();
        if (img != null)
        {
            img.enabled = false;
        }
    }

    /// <summary>
    /// Verifica se o estágio foi concluído (grid e bandeja vazios) ou se o jogador ficou preso.
    /// </summary>

    void VerificarVitoriaDoEstagio()
    {
        // Se o jogo já terminou, não faz mais nada para evitar chamadas duplicadas
        if (jogoTerminou) return;

        // Condição 1: Verifica se ainda há monstros no grid
        bool monstrosRestantesNoGrid = Monstros.Any(coluna => coluna.Any(m => m != null && m.cor != -1));
        if (monstrosRestantesNoGrid) return; // Se há monstros, o estágio não acabou

        // Condição 2: Verifica se a bandeja está vazia
        bool bandejaVazia = Armazem.Count == 0;
        if (!bandejaVazia)
        {
            // Se o grid está vazio mas a bandeja não, checa por soft-lock
            var contagemDeCores = Armazem.GroupBy(m => m.cor).ToDictionary(g => g.Key, g => g.Count());
            bool combinacaoPossivel = contagemDeCores.Any(par => par.Value >= 3);
            if (!combinacaoPossivel)
            {
                Derrota("Sem combinações possíveis!");
            }
            return; // Se há peças mas combinações são possíveis, o estágio não acabou
        }

        // Verifica se o estágio que ACABAMOS de concluir era o último.
        // Usamos "+ 1" porque os índices da lista começam em 0.
        if (estagioAtual + 1 >= levelGroupAtual.estagios.Count)
        {
            // Se era o último, é vitória!
            Vitoria("Nível Concluído!");
        }
        else
        {
            // Se não era o último, incrementa o contador e carrega o próximo.
            estagioAtual++;
            StartCoroutine(CarregarProximoEstagioComDelay(1.5f));
        }
    }

    /// <summary>
    /// Corrotina para criar uma pausa antes de carregar o próximo estágio.
    /// </summary>

    IEnumerator CarregarProximoEstagioComDelay(float delay)
    {
        // Trava o jogo durante a transição para evitar cliques indesejados
        jogoTerminou = true;

        yield return new WaitForSeconds(delay); // Espera pelo tempo definido

        // Destrava o jogo e inicia o próximo estágio
        jogoTerminou = false;
        StartCoroutine(InicializarEstagioAposLayout(estagioAtual));
    }

    /// <summary>
    /// Ativa o estado de vitória do jogo.
    /// </summary>

    void Vitoria(string mensagem)
    {
        if (jogoTerminou) return;
        jogoTerminou = true;
        Debug.Log("VITÓRIA: " + mensagem);
        if (painelVitoria != null) painelVitoria.SetActive(true);
    }

    /// <summary>
    /// Ativa o estado de derrota do jogo.
    /// </summary>

    void Derrota(string mensagem)
    {
        if (jogoTerminou) return;
        jogoTerminou = true;
        Debug.Log("DERROTA: " + mensagem);
        if (painelDerrota != null) painelDerrota.SetActive(true);
    }

    /// <summary>
    /// Procura por grupos de 3 ou mais na bandeja, remove-os dos dados e atualiza a UI.
    /// </summary>

    void ChecarEEliminarGrupos()
    {
        var gruposParaRemover = Armazem.GroupBy(m => m.cor)
                                     .Where(g => g.Sum(m=> PegarTipoDeMonstro(m)) >= 3)
                                     .ToList();
        if (gruposParaRemover.Any())
        {
            foreach (var grupo in gruposParaRemover)
            {
                List<Monstro> aRemover = grupo.Take(3).ToList();
                foreach (var monstro in aRemover)
                {
                    Armazem.Remove(monstro);
                }
            }
            AtualizarUIArmazem();
            VerificarVitoriaDoEstagio();
        }
    }

    // --- MÉTODOS DE REGRAS DO JOGO ---

    /// <summary>
    /// Verifica se um monstro pode ser removido com base nas regras de bloqueio.
    /// </summary>

    bool PodeRemover(Monstro monstro)
    {
        // Verifica se a parte principal do monstro tem um caminho livre.
        if (!TemCaminhoLivre(monstro.posicaoGrid.Item1, monstro.posicaoGrid.Item2))
        {
            return false;
        }

        // Se for uma peça de 2 partes, verifica a segunda parte também.
        if (monstro.segundaParte != null)
        {
            if (!TemCaminhoLivre(monstro.segundaParte.posicaoGrid.Item1, monstro.segundaParte.posicaoGrid.Item2))
            {
                return false;
            }
        }



        // Adicione aqui a verificação para 'terceiraParte' se necessário.



        // Se TODAS as partes da peça têm um caminho livre, ela pode ser removida.

        if (monstro.escondido)
        {
            monstro.escondido = false; // Revela a peça
            return false; // Não remove a peça, apenas revela
        }

        return true;
    }

    /// <summary>
    /// Verifica se uma peça em uma posição tem um "caminho de fuga" até a borda de baixo.
    /// ISSO VERIFICA SÓ OS LADOS, NÃO O CAMINHO
    /// </summary>

    private bool TemCaminhoLivre(int x, int y) // x = 4 , y = 4
    {
        if (y == 0)
        {
            return true;
        }

        int[] dx = { 0, 0, 1, -1 }; // Vizinhos na horizontal
        int[] dy = { 1, -1, 0, 0 }; // Vizinhos na vertical

        for (int i = 0; i < 4; i++)
        {
            int vizinhoX = x + dx[i];
            int vizinhoY = y + dy[i];
            // [3,3] [3,5] [5,3] [ 5 ,5 ]
            if (vizinhoX >= 0 && vizinhoX < gridWidth && vizinhoY >= 0 && vizinhoY < gridHeight)
            {
                if (Monstros[vizinhoX][vizinhoY] == null)
                {
                    //if (BuscaPorCaminhoAteBorda(vizinhoX, vizinhoY) && Monstros[x][y].escondido )
                    //{
                    //    // E caso seja uma peça de caixa, vai liberar a caixa. E mostrar para oq veio ao mundo!!!!
                    //    if (Monstros[x][y].escondido)
                    //    {
                    //        Monstros[x][y].escondido = false;
                    //        Monstros[x][y].GetComponent<Image>().sprite = spriteCaixa; // Sprite da caixa revelada
                    //    }
                    //    return false; // Ele n vai ser removido, mas vai liberar a caixa, pro próximo click porra!!!!!
                    //}
                    if (BuscaPorCaminhoAteBorda(vizinhoX, vizinhoY))
                    {
                        // Se encontrou um caminho livre até a borda de baixo, retorna verdadeiro.
                        if (Monstros[x][y].escondido)
                        {
                            Monstros[x][y].escondido = false;
                            Monstros[x][y].GetComponent<Image>().sprite = spriteCaixa; // Sprite da caixa revelada
                        }
                        return true;
                    }
                }
            }
        }

        // Se não está na borda de baixo e não encontrou nenhum caminho, está presa.
        return false;
    }

    /// <summary>
    /// Algoritmo de Busca em Largura (BFS) para encontrar um caminho de células vazias até a borda inferior.
    /// ESSE É O QUE REALMENTE BUSCA O CAMINHO ATÉ A BORDA!
    /// </summary>

    private bool BuscaPorCaminhoAteBorda(int startX, int startY)
    {
        var fila = new Queue<Tuple<int, int>>();
        var visitados = new HashSet<Tuple<int, int>>();

        var posInicial = new Tuple<int, int>(startX, startY);
        fila.Enqueue(posInicial);
        visitados.Add(posInicial);

        while (fila.Count > 0)
        {
            var pos = fila.Dequeue();
            int currentX = pos.Item1;
            int currentY = pos.Item2;

            if (currentY == 0)
            {
                return true; // Caminho para a borda de baixo encontrado!
            }

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nextX = currentX + dx[i];
                int nextY = currentY + dy[i];
                var proximaPos = new Tuple<int, int>(nextX, nextY);

                if (nextX >= 0 && nextX < gridWidth && nextY >= 0 && nextY < gridHeight &&
                    !visitados.Contains(proximaPos) && Monstros[nextX][nextY] == null)
                {
                    visitados.Add(proximaPos);
                    fila.Enqueue(proximaPos);
                }
            }
        }
        return false;
    }

    // --- MÉTODOS DE ATUALIZAÇÃO VISUAL ---

    /// <summary>
    /// Redesenha a UI da bandeja com base na lista de dados 'Armazem'.
    /// </summary>

    void AtualizarUIArmazem()
    {
        if (armazemUIParent == null) return;

        // 1. Destrói todos os ícones que estão atualmente na bandeja.
        foreach (Transform child in armazemUIParent)
        {
            Destroy(child.gameObject);
        }

        // Se a bandeja estiver vazia, não há mais nada a fazer.
        if (Armazem.Count == 0) return;

        // 2. Itera pela lista de dados 'Armazem' e instancia o prefab correto para cada monstro.
        foreach (Monstro monstro in Armazem)
        {
            GameObject prefabParaInstanciar;
            Sprite spriteParaMostrar;

            int tipoMonstro = PegarTipoDeMonstro(monstro);

            // Decide qual prefab e qual sprite usar
            if (tipoMonstro == 2)
            {
                prefabParaInstanciar = iconeBandejaBisPrefab;
                spriteParaMostrar = spritesDesbloqueadosBis[monstro.cor];
            }
            else // Tipo 1 (e qualquer outro caso)
            {
                prefabParaInstanciar = iconeBandejaNormalPrefab;
                spriteParaMostrar = monstroSpritesBandeira[monstro.cor];
            }

            // 3. Instancia o prefab escolhido como filho da bandeja.
            GameObject novoIcone = Instantiate(prefabParaInstanciar, armazemUIParent);
            novoIcone.name = $"Icone_{monstro.cor}" + (tipoMonstro == 2 ? "_Bis" : "");

            // 4. Configura a imagem do ícone recém-criado.
            Image iconeImage = novoIcone.GetComponent<Image>();
            if (iconeImage != null)
            {
                iconeImage.sprite = spriteParaMostrar;
            }
        }
    }

    /// <summary>
    /// Varre todo o grid e atualiza o sprite de cada peça para o estado
    /// "bloqueado" ou "desbloqueado" com base nas regras do jogo.
    /// </summary>
    void AtualizarVisualsDoGrid()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Monstro monstro = Monstros[x][y];
                

                // Se não houver monstro nesta célula, pula para a próxima
                if (monstro == null) continue;
                int tipoMonstro = PegarTipoDeMonstro(monstro);
                if( tipoMonstro == 2)
                {
                    // LOGICA DO BI
                    //Verificar se alguma das 2 partes estão livres
                    bool primeiraParteLivre = PodeRemover(monstro);

                    bool segundaParteLivre = PodeRemover(monstro.segundaParte);
                    Debug.Log($"Segunda parte do monstrão {monstro.segundaParte.posicaoGrid}  e a primeira aqui {monstro.posicaoGrid}");
                    // Se alguma das partes estiver livre, a peça é desbloqueada
                    bool estaJoia = primeiraParteLivre || segundaParteLivre;

                    monstro.AtualizarEstadoDeAnimacao(estaJoia);
                    monstro.segundaParte.AtualizarEstadoDeAnimacao(estaJoia);

                    // Define o sprite correto para a peça de 2 partes
                    Image pecaImageMonstroPrincipal = monstro.GetComponent<Image>();
                    if (pecaImageMonstroPrincipal == null) { 
                        Debug.Log("A imagem do monstro principal é nula");
                        continue; 
                    }
                    Image pecaImageSegundoMonstro = monstro.segundaParte.GetComponent<Image>();
                    if (pecaImageSegundoMonstro == null)
                    {
                        Debug.Log("A imagem do monstro secundario é nula");
                        continue;
                    }
                    // Seleciona a lista de sprites correta (bloqueado ou desbloqueado)
                    List<Sprite> spriteListBis= estaJoia ? spritesDesbloqueadosBis : spritesBloqueadosBis;
                    Debug.Log("Sim está joia para liberar o bis para a destrução");
                    // Pega o sprite correto da lista com base na ecor/tipo da peça

                    pecaImageMonstroPrincipal.sprite = spriteListBis[monstro.cor];
                    Debug.Log($"Imagem selecionada para o bis principal foi {monstro.cor.ToString()}");
                    pecaImageSegundoMonstro.sprite = spriteListBis[monstro.segundaParte.cor];
                    Debug.Log($"Imagem selecionada para o bis secundario foi {monstro.segundaParte.cor.ToString()}");
                    



                    continue;
                }
                if ( tipoMonstro == 3)
                {
                    // LOGICA DO TRI
                    continue; // Não faz nada para monstros de 3 partes por enquanto

                }

                // Encontra a imagem da peça
                Image pecaImage = monstro.GetComponent<Image>();
                if (pecaImage == null) continue;

                // Decide se a peça está bloqueada ou não usando a regra de jogo
                bool estaLivre = PodeRemover(monstro);

                monstro.AtualizarEstadoDeAnimacao(estaLivre);

                // Seleciona a lista de sprites correta (bloqueado ou desbloqueado)
                List<Sprite> spriteList = estaLivre ? spritesDesbloqueados : spritesBloqueados;

                // Pega o sprite correto da lista com base na cor/tipo da peça
                // (Esta lógica de índice deve corresponder à sua lista de sprites)
                Sprite spriteParaMostrar = null;
                // Exemplo de como pegar o sprite. Adapte os índices se necessário.
                // Índices 0-5: Cores, 6: Caixa, 7: Parede, 8: Gerador
                if (monstro.escondido)
                {
                    spriteParaMostrar = spriteCaixa;
                }
                else if (monstro.cor == -1) // Parede
                {
                    spriteParaMostrar = spriteParede;
                }
                else
                {
                    spriteParaMostrar = spriteList[monstro.cor];
                }

                // Define o sprite na imagem
                pecaImage.sprite = spriteParaMostrar;
            }
        }
    }

#if UNITY_EDITOR
    // --- MÉTODOS EXCLUSIVOS DO EDITOR ---

    /// <summary>
    /// Desenha uma pré-visualização do nível na janela Scene para facilitar o level design.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (levelGroupAtual == null || levelGroupAtual.estagios == null || levelGroupAtual.estagios.Count == 0 || boardContainer == null)
            return;

        // Garante que o índice de visualização seja válido
        if (estagioParaVisualizar >= levelGroupAtual.estagios.Count)
            estagioParaVisualizar = levelGroupAtual.estagios.Count - 1;
        if (estagioParaVisualizar < 0)
            estagioParaVisualizar = 0;

        LevelData nivelAtual = levelGroupAtual.estagios[estagioParaVisualizar];
        if (nivelAtual == null || nivelAtual.layoutDoGrid == null) return;

        Vector3[] corners = new Vector3[4];
        boardContainer.GetWorldCorners(corners);
        float containerWorldWidth = Vector3.Distance(corners[0], corners[3]);
        float containerWorldHeight = Vector3.Distance(corners[0], corners[1]);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);

        int gridHeight = nivelAtual.layoutDoGrid.Count;
        if (gridHeight == 0) return;
        int gridWidth = nivelAtual.layoutDoGrid[0].colunas.Count;
        if (gridWidth == 0) return;

        float cellWidthPotential = containerWorldWidth / gridWidth;
        float cellHeightPotential = containerWorldHeight / gridHeight;
        float cellSize = Mathf.Min(cellWidthPotential, cellHeightPotential);
        float totalGridWidth = cellSize * gridWidth;
        float totalGridHeight = cellSize * gridHeight;
        Vector3 containerCenter = corners[0] + new Vector3(containerWorldWidth / 2, containerWorldHeight / 2, 0);
        float startX = containerCenter.x - (totalGridWidth / 2) + (cellSize / 2);
        float startY = containerCenter.y - (totalGridHeight / 2) + (cellSize / 2);

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int linhaDoLayout = (gridHeight - 1) - y;
                LevelData.TileConfig tile = nivelAtual.layoutDoGrid[linhaDoLayout].colunas[x];
                float posX = startX + x * cellSize;
                float posY = startY + y * cellSize;
                Vector3 position = new Vector3(posX, posY, containerCenter.z);

                Gizmos.color = new Color(1, 1, 1, 0.1f);
                Gizmos.DrawWireCube(position, new Vector3(cellSize, cellSize, 0.01f));
                float gizmoSize = cellSize * 0.9f;
                switch (tile.tipo)
                {
                    case LevelData.TipoDeTile.Monstro:
                        Gizmos.color = GetColorForGizmo(tile.corDoMonstro);
                        if (tile.escondido) Gizmos.DrawCube(position, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                        else Gizmos.DrawSphere(position, gizmoSize / 2);
                        break;
                    case LevelData.TipoDeTile.Parede:
                        Gizmos.color = Color.gray;
                        Gizmos.DrawCube(position, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                        break;
                    case LevelData.TipoDeTile.Gerador:
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawCube(position, new Vector3(gizmoSize * 0.8f, gizmoSize * 0.8f, gizmoSize * 0.8f));
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Retorna uma cor para o Gizmo com base no índice de cor da peça.
    /// </summary>
    private Color GetColorForGizmo(int corIndex)
    {
        switch (corIndex % 6)
        {
            case 0: return Color.red;
            case 1: return Color.green;
            case 2: return Color.blue;
            case 3: return Color.yellow;
            case 4: return Color.cyan;
            case 5: return Color.white;
            default: return Color.black;
        }
    }
#endif

    /// <summary>
    /// Ajusta a câmera principal para enquadrar o grid (útil para testes fora do modo UI).
    /// </summary>

    void AjustarCameraParaOGrid()
    {
        if (levelGroupAtual == null || levelGroupAtual.estagios == null || levelGroupAtual.estagios.Count == 0) return;
        LevelData nivelAtual = levelGroupAtual.estagios[0]; // Ajusta a câmera com base no primeiro estágio
        if (nivelAtual == null) return;

        int gridHeight = nivelAtual.layoutDoGrid.Count;
        int gridWidth = (gridHeight > 0) ? nivelAtual.layoutDoGrid[0].colunas.Count : 0;
        if (gridWidth == 0 || gridHeight == 0) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.orthographic) return;

        float centerX = (float)(gridWidth - 1) / 2.0f;
        float centerY = (float)(gridHeight - 1) / 2.0f;
        mainCamera.transform.position = new Vector3(centerX, centerY, mainCamera.transform.position.z);
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float gridRatio = (float)gridWidth / (float)gridHeight;
        if (screenRatio >= gridRatio)
        {
            mainCamera.orthographicSize = (float)gridHeight / 2.0f + 1.0f;
        }
        else
        {
            mainCamera.orthographicSize = ((float)gridWidth / 2.0f) / screenRatio + 1.0f;
        }
    }
}