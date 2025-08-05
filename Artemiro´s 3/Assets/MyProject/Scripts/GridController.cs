using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GridController : MonoBehaviour
{
    [Header("Configuração do Nível")]
    public List<LevelData> Leveis;
    public int levelAtual;
    public float tempoInicialDoNivel = 60f;
    private float tempoRestante;
    private int totalMonstros;
    [Header("Configuração do Grid")]
    private int gridWidth;
    private int gridHeight;

    [Header("UI do Relógio")]
    public RelogioUIController relogioVisual;

    [Header("Prefabs")]
    public GameObject gridCellPrefab; // Arraste aqui o Prefab da célula de fundo
    public List<GameObject> monstroPrefab;
    public GameObject paredePrefab;
    public GameObject prefabCaixa; // Prefab para monstros escondidos (caixas)
    public GameObject prefabGerador; // Prefab para o gerador de monstros
    [Header("UI do Armazém")]
    // TODO : Deixar de ser UI para algo real no jogo
    // TODO : GRID para o armazém
    public Transform armazemUIParent; // Arraste o painel da UI para cá
    public GameObject monstroIconPrefab; // Arraste o prefab do ícone do monstro para cá
    public List<Sprite> monstroSprites; // Lista de sprites, um para cada cor de monstro

    [Header("Estado do Jogo")]
    public List<List<Monstro>> Monstros = new List<List<Monstro>>();
    public List<Monstro> Armazem = new List<Monstro>();
    public RectTransform boardArmazem; // Referência ao container do tabuleiro
    private int maxArmazem = 7;
    private float xInicialArmazem = 0f; // Posição inicial do armazém
    private float yInicialArmazem = 0f; // Posição inicial do armazém
    private float cellSizeArmazem = 0f; // Tamanho da célula do armazém
    [Header("Configuração da UI do Tabuleiro")]
    public RectTransform boardContainer; // Arraste o seu objeto BoardContainer aqui pelo Inspector

    [Header("Referências para o Raycast da UI")]
    public GraphicRaycaster graphicRaycaster; // Arraste aqui o componente do seu Canvas
    public EventSystem eventSystem;           // Arraste aqui o objeto EventSystem da sua cena

    private bool perdeu = false;
    private bool ganhou = false;


    // Use this for initialization
    void Start()
    {
        tempoInicialDoNivel = Leveis[this.levelAtual].tempoMaximo;
        InicializarArmazem(Leveis[this.levelAtual]);
        this.tempoRestante = Leveis[this.levelAtual].tempoMaximo;
        StartCoroutine(InicializarGridAposLayout(Leveis[this.levelAtual]));
    }

    void InicializarArmazem(LevelData nivelAtual)
    {

        maxArmazem = nivelAtual.TamanhoInventario;

        if (armazemUIParent == null)
        {
            Debug.LogError("O Armazém UI Parent não foi atribuído no GridController!");
            return;
        }

        // 1. Pega as dimensões do container da UI. Ex: 1000px de largura, 800px de altura.
        float containerWidth = boardArmazem.rect.width;
        float containerHeight = boardArmazem.rect.height;

        // 2. Calcula a largura e altura MÁXIMA que uma célula poderia ter.
        // Ex: Se o grid é 10x10 em um container 1000x800, cellWidth = 100, cellHeight = 80.
        float cellWidth = containerWidth / maxArmazem;
        float cellHeight = containerHeight;

        // 3. Pega o MENOR desses dois valores. Isso GARANTE que a célula será um QUADRADO.
        // No nosso exemplo, cellSize = Mathf.Min(100, 80) => 80.
        this.cellSizeArmazem = Mathf.Min(cellWidth, cellHeight);

        // 4. Calcula a área total que o nosso grid de células quadradas irá ocupar.
        // Ex: totalGridWidth = 80 * 10 = 800. totalGridHeight = 80 * 10 = 800.
        float totalGridWidth = this.cellSizeArmazem * maxArmazem;
        float totalGridHeight = this.cellSizeArmazem;

        // 5. Calcula a posição inicial para CENTRALIZAR o grid.
        // Para um pivô central (0.5, 0.5), o cálculo nos dá a coordenada do centro da primeira célula (canto inferior esquerdo).
        // Ex: startX = -(800 / 2) + (80 / 2) = -400 + 40 = -360.
        this.xInicialArmazem = -(totalGridWidth + this.cellSizeArmazem) /2;
        this.yInicialArmazem = -(totalGridHeight + this.cellSizeArmazem) /2;

        // Inicializa a lista de monstros no armazém
        Armazem.Clear();
           
        
    }

    void InicializarGrid(LevelData nivelAtual)
    {
        if (nivelAtual == null || nivelAtual.layoutDoGrid == null)
        {
            Debug.LogError("Nenhum LevelData válido foi atribuído ao GridController!");
            return;
        }

        if (boardContainer == null)
        {
            Debug.LogError("O BoardContainer não foi atribuído no GridController!");
            return;
        }

        // Limpa o tabuleiro de qualquer execução anterior
        foreach (Transform child in boardContainer)
        {
            Destroy(child.gameObject);
        }

        // Define as dimensões lógicas do grid
        gridHeight = nivelAtual.layoutDoGrid.Count;
        gridWidth = (gridHeight > 0) ? nivelAtual.layoutDoGrid[0].colunas.Count : 0;

        // Inicializa a matriz de dados que guarda o estado do jogo
        Monstros = new List<List<Monstro>>();
        for (int i = 0; i < gridWidth; i++)
        {
            Monstros.Add(new List<Monstro>(new Monstro[gridHeight]));
        }

        // --- Início do Cálculo de Layout Responsivo ---

        // 1. Pega as dimensões do container da UI. Ex: 1000px de largura, 800px de altura.
        float containerWidth = boardContainer.rect.width;
        float containerHeight = boardContainer.rect.height;

        // 2. Calcula a largura e altura MÁXIMA que uma célula poderia ter.
        // Ex: Se o grid é 10x10 em um container 1000x800, cellWidth = 100, cellHeight = 80.
        float cellWidth = containerWidth / gridWidth;
        float cellHeight = containerHeight / gridHeight;

        // 3. Pega o MENOR desses dois valores. Isso GARANTE que a célula será um QUADRADO.
        // No nosso exemplo, cellSize = Mathf.Min(100, 80) => 80.
        float cellSize = Mathf.Min(cellWidth, cellHeight);

        // 4. Calcula a área total que o nosso grid de células quadradas irá ocupar.
        // Ex: totalGridWidth = 80 * 10 = 800. totalGridHeight = 80 * 10 = 800.
        float totalGridWidth = cellSize * gridWidth;
        float totalGridHeight = cellSize * gridHeight;

        // 5. Calcula a posição inicial para CENTRALIZAR o grid.
        // Para um pivô central (0.5, 0.5), o cálculo nos dá a coordenada do centro da primeira célula (canto inferior esquerdo).
        // Ex: startX = -(800 / 2) + (80 / 2) = -400 + 40 = -360.
        float startX = -(totalGridWidth / 2) + (cellSize / 2);
        float startY = -(totalGridHeight / 2) + (cellSize / 2);

        // --- Início da Construção Visual do Grid ---
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // Calcula a posição do centro da célula atual
                float posX = startX + x * cellSize;
                float posY = startY + y * cellSize;

                int linhaDoLayout = (gridHeight - 1) - y;
                LevelData.TileConfig tile = nivelAtual.layoutDoGrid[linhaDoLayout].colunas[x];

                // Instancia e posiciona a célula de fundo (opcional)
                if (gridCellPrefab != null)
                {
                    GameObject cell = Instantiate(gridCellPrefab, boardContainer);
                    cell.name = $"Grid Cell ({x}, {y})";
                    RectTransform cellRect = cell.GetComponent<RectTransform>();
                    if (cellRect != null)
                    {
                        // Usa o cellSize para garantir que a célula de fundo também seja quadrada
                        cellRect.sizeDelta = new Vector2(cellSize, cellSize);
                        cellRect.anchoredPosition = new Vector2(posX, posY);
                    }
                }

                if (tile.tipo == LevelData.TipoDeTile.Vazio)
                {
                    continue;
                }

                // Lógica para escolher o prefab a ser instanciado
                GameObject prefabParaInstanciar = null;
                switch (tile.tipo)
                {
                    case LevelData.TipoDeTile.Monstro:
                        prefabParaInstanciar = tile.escondido ? prefabCaixa : monstroPrefab[tile.corDoMonstro%6];
                        this.totalMonstros++;
                        break;
                    case LevelData.TipoDeTile.Parede:
                        prefabParaInstanciar = paredePrefab;
                        break;
                    case LevelData.TipoDeTile.Gerador:
                        prefabParaInstanciar = prefabGerador;
                        break;
                }

                if (prefabParaInstanciar != null)
                {
                    GameObject obj = Instantiate(prefabParaInstanciar, boardContainer);
                    obj.name = $"{tile.tipo} ({x}, {y})";

                    RectTransform objRect = obj.GetComponent<RectTransform>();
                    if (objRect != null)
                    {
                        // Define o tamanho da peça para ser um quadrado, com um pequeno respiro visual
                        objRect.sizeDelta = new Vector2(cellSize * 0.95f, cellSize * 0.95f);
                        objRect.anchoredPosition = new Vector2(posX, posY);
                    }

                    // Configura o componente Monstro com os dados do nível
                    Monstro monstroComponent = obj.GetComponent<Monstro>();
                    if (monstroComponent != null)
                    {
                        monstroComponent.posicaoGrid = new Tuple<int, int>(x, y);
                        monstroComponent.escondido = tile.escondido;
                        monstroComponent.cor = (tile.tipo == LevelData.TipoDeTile.Parede) ? -1 : tile.corDoMonstro;
                        monstroComponent.gridController = this;

                        if (tile.tipo == LevelData.TipoDeTile.Gerador && obj.TryGetComponent(out GeradorDeMonstros geradorComponent))
                        {
                            geradorComponent.monstrosParaGerar = tile.MonstrosASeremGeradosPeloGerador;
                        }

                        Monstros[x][y] = monstroComponent;
                    }
                }
            }
        }
    }

    IEnumerator InicializarGridAposLayout(LevelData nivelAtual)
    {
        // Espera até o final do frame atual. Neste ponto, todos os cálculos
        // de layout da UI para este frame já foram concluídos.
        yield return new WaitForEndOfFrame();

        // Agora que temos certeza que o container tem seu tamanho final,
        // podemos chamar nosso método de inicialização com segurança.
        InicializarGrid(nivelAtual);
    }

    // Update is called once per frame
    void Update()
    {
        // Atualiza o tempo restante

        if (tempoRestante > 0 && !perdeu && !ganhou)
        {
            tempoRestante -= Time.deltaTime;

            // ATUALIZA O RELÓGIO VISUAL (a nova linha)
            if (relogioVisual != null)
            {
                relogioVisual.AtualizarDisplay(tempoRestante, tempoInicialDoNivel);
            }

            // Verificar se o tempo acabou

            
        }
        else if( !perdeu && !ganhou )
        {
            tempoRestante = 0;
            perdeu = true;

            // Logica de derrota por tempo esgotado

            Debug.Log("Tempo esgotado! Você perdeu!");


        }

        if (perdeu)
        {
            //logica de derrota
        }

        if( ganhou)
        {
            //logica de vitória
            this.levelAtual++;
            tempoInicialDoNivel = Leveis[this.levelAtual].tempoMaximo;
            InicializarArmazem(Leveis[this.levelAtual]);
            this.tempoRestante = Leveis[this.levelAtual].tempoMaximo;
            StartCoroutine(InicializarGridAposLayout(Leveis[this.levelAtual]));
        }



        HandleClick();
    }

    void HandleClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(pointerEventData, results);

            if (results.Count > 0)
            {
                // --- INÍCIO DA CORREÇÃO ---
                // Procura o componente no objeto clicado E NOS SEUS PAIS.
                Monstro monstroClicado = results[0].gameObject.GetComponentInParent<Monstro>();
                // --- FIM DA CORREÇÃO ---

                // Agora, a verificação 'if (monstroClicado != null)' vai funcionar corretamente.
                if (monstroClicado != null)
                {
                    Debug.Log($"Clicou no objeto da UI: {monstroClicado.gameObject.name}");

                    if (monstroClicado.vazio) return;
                    if (monstroClicado.cor == -1) return;

                    bool podeRemover = PodeRemover(monstroClicado);
                    Debug.Log($"Pode remover: {podeRemover}");
                    if (podeRemover && Armazem.Count < maxArmazem)
                    {
                        this.totalMonstros--;
                        if(!ganhou && this.totalMonstros == 0)
                        {
                            ganhou = true;
                            Debug.Log("Parabéns! Você ganhou!");
                        }
                        AdicionarAoArmazem(monstroClicado);
                        RemoverDoGrid(monstroClicado);

                    }

                }
            }
        }
    }

    bool PodeRemover(Monstro monstro)
    {
        if(perdeu) return false; // Se já perdeu, não pode remover mais nada

        int x = monstro.posicaoGrid.Item1;
        int yParteMaisBaixa = monstro.posicaoGrid.Item2;

        if (monstro.segundaParte != null)
        {
            yParteMaisBaixa = Mathf.Min(yParteMaisBaixa, monstro.segundaParte.posicaoGrid.Item2);
        }

        if (yParteMaisBaixa <= 0) return true; // Já está na borda inferior

        var visitados = new HashSet<Tuple<int, int>>();
        var fila = new Queue<Tuple<int, int>>();
        
        var posInicial = new Tuple<int, int>(x, yParteMaisBaixa);
        fila.Enqueue(posInicial);
        visitados.Add(posInicial);

        while (fila.Count > 0)
        {
            var pos = fila.Dequeue();
            if (pos.Item2 <= 0) {

                return true; // Chegou na borda
            }

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = pos.Item1 + dx[i];
                int ny = pos.Item2 + dy[i];
                var proximaPos = new Tuple<int, int>(nx, ny);

                if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight && !visitados.Contains(proximaPos))
                {
                    var proximaCelula = Monstros[nx][ny];
                    if ((proximaCelula == null || proximaCelula.vazio) && proximaCelula?.cor != -1 )
                    {
                        
                        visitados.Add(proximaPos);
                        fila.Enqueue(proximaPos);
                    }
                    if (proximaCelula is not null && proximaCelula.escondido)
                    {
                        GameObject ElementoDoMonstro = proximaCelula.gameObject;
                        // Revela o monstro escondido
                        Destroy(ElementoDoMonstro); // Remove a caixa
                        GameObject monstroRevelado = Instantiate(monstroPrefab[proximaCelula.cor], proximaCelula.transform.position, Quaternion.identity, this.transform);
                        monstroRevelado.name = $"Monstro Revelado ({proximaCelula.cor})";
                        Monstro novoMonstro = monstroRevelado.GetComponent<Monstro>();
                        novoMonstro.posicaoGrid = proximaCelula.posicaoGrid;
                        novoMonstro.vazio = false;
                        novoMonstro.escondido = false; // Marca como revelado
                        novoMonstro.cor = proximaCelula.cor; // Mantém a cor do monstro
                        Monstros[proximaCelula.posicaoGrid.Item1][proximaCelula.posicaoGrid.Item2] = novoMonstro;
                    }
                }

            }
        }
        return false;
    }

    void RemoverDoGrid(Monstro monstro)
    {
        // TODO : Animação de remoção
        
        Monstros[monstro.posicaoGrid.Item1][monstro.posicaoGrid.Item2] = null;
        if (monstro.segundaParte != null)
        {
            var pos2 = monstro.segundaParte.posicaoGrid;
            Monstros[pos2.Item1][pos2.Item2] = null;
            Destroy(monstro.segundaParte.gameObject);
        }
        Destroy(monstro.gameObject);
    }

    void AdicionarAoArmazem(Monstro monstro)
    {
        // Garante que monstros de 2 partes não sejam adicionados duas vezes
        if (!Armazem.Contains(monstro)) {
            Armazem.Add(monstro);
            monstro.gameObject.SetActive(false); // Esconde o monstro em vez de mover

        }
        
        Armazem.Sort((a, b) => a.cor.CompareTo(b.cor));
        AtualizarUIArmazem(); // Atualiza a UI para mostrar o monstro adicionado

        ChecarEEliminarGrupos();

        if( Armazem.Count == maxArmazem)
        {
            perdeu = true;
        }

    }

    void ChecarEEliminarGrupos()
    {
        var gruposParaRemover = Armazem.GroupBy(m => m.cor)
                                     .Where(g => g.Count() >= 3)
                                     .ToList(); // Evita erro ao modificar a lista durante a iteração
        foreach (var grupo in gruposParaRemover)
        {
            Debug.Log($"Grupo encontrado: Cor {grupo.Key}, Tamanho {grupo.Count()}");
        }
        bool foiRemovidoAlgo = false;
        foreach (var grupo in gruposParaRemover)
        {
            foiRemovidoAlgo = true;
            List<Monstro> aRemover = grupo.Take(3).ToList();
            foreach (var monstro in aRemover)
            {
                Armazem.Remove(monstro);
                //Destroy(monstro.gameObject);
            }
        }

        if (foiRemovidoAlgo)
        {
            // Poderíamos ter um delay ou animação aqui no futuro
            AtualizarUIArmazem(); // Atualiza a UI após remover o grupo
        }
    }

    void AtualizarUIArmazem()
    {
        if (armazemUIParent == null) return;

        // Limpa os ícones antigos
        foreach (Transform child in armazemUIParent)
        {
            Destroy(child.gameObject);
        }

        // Cria os novos ícones para cada monstro no armazém
        if (monstroIconPrefab == null) return;

        foreach (Monstro monstro in Armazem)
        {
            
            GameObject monstroIcon = Instantiate(monstroIconPrefab, armazemUIParent);
            UnityEngine.UI.Image imageComponent = monstroIcon.GetComponent<UnityEngine.UI.Image>();
            
            // Atribui o sprite correto baseado na cor do monstro
            if (imageComponent != null && monstro.cor >= 0 && monstro.cor < monstroSprites.Count)
            {
                imageComponent.sprite = monstroSprites[monstro.cor];
            }
            else if (imageComponent != null)
            {
                // Deixa em branco ou mostra um sprite padrão se a cor for inválida
                imageComponent.sprite = null; 
            }
        }
    }

//#if UNITY_EDITOR
//    private void OnDrawGizmos()
//    {
//        LevelData nivelAtual = this.Leveis[levelAtual];
//        if (nivelAtual == null || nivelAtual.layoutDoGrid == null || boardContainer == null)
//        {
//            return;
//        }

//        // 1. Obter os 4 cantos do container no espaço do mundo.
//        Vector3[] corners = new Vector3[4];
//        boardContainer.GetWorldCorners(corners);

//        // 2. Calcular a largura e altura do container no espaço do mundo.
//        float containerWorldWidth = Vector3.Distance(corners[0], corners[3]);
//        float containerWorldHeight = Vector3.Distance(corners[0], corners[1]);

//        // 3. Desenhar a borda do container para referência visual.
//        Gizmos.color = Color.white;
//        Gizmos.DrawLine(corners[0], corners[1]); // Borda Esquerda
//        Gizmos.DrawLine(corners[1], corners[2]); // Borda Superior
//        Gizmos.DrawLine(corners[2], corners[3]); // Borda Direita
//        Gizmos.DrawLine(corners[3], corners[0]); // Borda Inferior

//        // 4. Reutilizar a mesma lógica de cálculo de InicializarGrid.
//        int gridHeight = nivelAtual.layoutDoGrid.Count;
//        if (gridHeight == 0) return;
//        int gridWidth = nivelAtual.layoutDoGrid[0].colunas.Count;
//        if (gridWidth == 0) return;

//        float cellWidthPotential = containerWorldWidth / gridWidth;
//        float cellHeightPotential = containerWorldHeight / gridHeight;
//        float cellSize = Mathf.Min(cellWidthPotential, cellHeightPotential);

//        float totalGridWidth = cellSize * gridWidth;
//        float totalGridHeight = cellSize * gridHeight;

//        Vector3 containerCenter = corners[0] + new Vector3(containerWorldWidth / 2, containerWorldHeight / 2, 0);
//        float startX = containerCenter.x - (totalGridWidth / 2) + (cellSize / 2);
//        float startY = containerCenter.y - (totalGridHeight / 2) + (cellSize / 2);

//        // 5. Desenhar o gizmo de cada célula.
//        for (int y = 0; y < gridHeight; y++)
//        {
//            for (int x = 0; x < gridWidth; x++)
//            {
//                int linhaDoLayout = (gridHeight - 1) - y;
//                LevelData.TileConfig tile = nivelAtual.layoutDoGrid[linhaDoLayout].colunas[x];

//                float posX = startX + x * cellSize;
//                float posY = startY + y * cellSize;
//                Vector3 position = new Vector3(posX, posY, containerCenter.z);

//                // --- NOVO: DESENHA A CÉLULA DE FUNDO PARA TODOS ---
//                Gizmos.color = new Color(1, 1, 1, 0.1f); // Cor cinza bem fraca
//                Gizmos.DrawWireCube(position, new Vector3(cellSize, cellSize, 0.01f));

//                float gizmoSize = cellSize * 0.9f;

//                switch (tile.tipo)
//                {
//                    case LevelData.TipoDeTile.Monstro:
//                        Gizmos.color = GetColorForGizmo(tile.corDoMonstro);
//                        if (tile.escondido)
//                        {
//                            Gizmos.DrawCube(position, new Vector3(gizmoSize, gizmoSize, gizmoSize));
//                        }
//                        else
//                        {
//                            Gizmos.DrawSphere(position, gizmoSize / 2);
//                        }
//                        break;

//                    // --- NOVO: DESENHA A PAREDE ---
//                    case LevelData.TipoDeTile.Parede:
//                        Gizmos.color = Color.gray;
//                        Gizmos.DrawCube(position, new Vector3(gizmoSize, gizmoSize, gizmoSize));
//                        break;

//                    // --- NOVO: DESENHA O GERADOR ---
//                    case LevelData.TipoDeTile.Gerador:
//                        Gizmos.color = Color.magenta;
//                        Gizmos.DrawCube(position, new Vector3(gizmoSize * 0.8f, gizmoSize * 0.8f, gizmoSize * 0.8f));
//                        break;
//                }
//            }
//        }
//    }

//    // Função auxiliar para obter cores diferentes para os monstros
//    private Color GetColorForGizmo(int corIndex)
//    {
//        switch (corIndex % 6) // Usa módulo para ciclar entre 6 cores
//        {
//            case 0: return Color.red;
//            case 1: return Color.green;
//            case 2: return Color.blue;
//            case 3: return Color.yellow;
//            case 4: return Color.cyan;
//            case 5: return Color.white;
//            default: return Color.black;
//        }
//    }
//#endif

//    void AjustarCameraParaOGrid()
//    {
//        LevelData nivelAtual = this.Leveis[levelAtual];
//        if (nivelAtual == null) return;

//        int gridHeight = nivelAtual.layoutDoGrid.Count;
//        int gridWidth = (gridHeight > 0) ? nivelAtual.layoutDoGrid[0].colunas.Count : 0;

//        if (gridWidth == 0 || gridHeight == 0) return;

//        Camera mainCamera = Camera.main;
//        if (mainCamera == null || !mainCamera.orthographic)
//        {
//            Debug.LogWarning("Câmera principal não encontrada ou não é ortográfica.");
//            return;
//        }

//        // Calcula o centro do grid
//        float centerX = (float)(gridWidth - 1) / 2.0f;
//        float centerY = (float)(gridHeight - 1) / 2.0f;
//        mainCamera.transform.position = new Vector3(centerX, centerY, mainCamera.transform.position.z);

//        // Ajusta o zoom da câmera
//        float screenRatio = (float)Screen.width / (float)Screen.height;
//        float gridRatio = (float)gridWidth / (float)gridHeight;

//        if (screenRatio >= gridRatio)
//        {
//            // A tela é mais larga que o grid, então a altura define o zoom
//            mainCamera.orthographicSize = (float)gridHeight / 2.0f + 1.0f; // +1 de padding
//        }
//        else
//        {
//            // O grid é mais largo que a tela, então a largura define o zoom
//            float newSize = ((float)gridWidth / 2.0f) / screenRatio + 1.0f; // +1 de padding
//            mainCamera.orthographicSize = newSize;
//        }
//    }

}
