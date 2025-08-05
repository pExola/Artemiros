using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class GridController : MonoBehaviour
{
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

    [Header("Prefabs")]
    public GameObject slotPrefab;
    public GameObject monstroIconPrefab;
    public List<Sprite> monstroSprites;

    [Header("Referências de Sistema")]
    public GraphicRaycaster graphicRaycaster;
    public EventSystem eventSystem;

    [Header("Debug")]
    [Tooltip("Qual estágio do LevelGroup deve ser desenhado na Scene? (0, 1, 2, etc.)")]
    [Range(0, 10)]
    public int estagioParaVisualizar = 0;

    // --- Variáveis de Estado do Jogo ---
    private List<List<Monstro>> Monstros = new List<List<Monstro>>();
    public List<Monstro> Armazem = new List<Monstro>();
    private int gridWidth, gridHeight, maxArmazem;
    private float tempoRestante;
    private int estagioAtual = 0;
    private bool jogoTerminou = false;

    void Start()
    {
        if (painelVitoria != null) painelVitoria.SetActive(false);
        if (painelDerrota != null) painelDerrota.SetActive(false);

        tempoRestante = tempoInicialDoNivel;
        StartCoroutine(InicializarEstagioAposLayout(estagioAtual));
    }

    void Update()
    {
        if (jogoTerminou) return;

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

    IEnumerator InicializarEstagioAposLayout(int indiceEstagio)
    {
        yield return new WaitForEndOfFrame();
        IniciarEstagio(indiceEstagio);
    }

    void IniciarEstagio(int indiceEstagio)
    {
        if (levelGroupAtual == null || levelGroupAtual.estagios.Count <= indiceEstagio)
        {
            Derrota("Erro de configuração de nível.");
            return;
        }

        LevelData nivelData = levelGroupAtual.estagios[indiceEstagio];
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

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int linhaDoLayout = y;
                LevelData.TileConfig tile = nivelData.layoutDoGrid[linhaDoLayout].colunas[x];

                // 1. Instancia o "Prefab Combo"
                GameObject slotObj = Instantiate(slotPrefab, boardContainer);
                slotObj.name = $"Slot ({x},{y})";

                // 2. Encontra as partes dentro do prefab instanciado (use os nomes exatos que você deu)
                Transform pecaTransform = slotObj.transform.Find("Peca_Monstro");
                if (pecaTransform == null)
                {
                    Debug.LogError("Objeto 'Peca_Monstro' não encontrado dentro do Slot_Prefab!", slotObj);
                    continue;
                }

                Image pecaImage = pecaTransform.GetComponent<Image>();
                Monstro monstroComponent = pecaTransform.GetComponent<Monstro>();

                // 3. Configura o slot com base nos dados do nível
                if (tile.tipo == LevelData.TipoDeTile.Vazio)
                {
                    pecaTransform.gameObject.SetActive(false); // Desativa só a peça
                    Monstros[x][y] = null; // Marca a posição como vazia
                }
                else
                {
                    Sprite spriteParaMostrar = null;
                    switch (tile.tipo)
                    {
                        case LevelData.TipoDeTile.Monstro:
                            // ATENÇÃO: A forma de pegar o sprite pode precisar de ajuste
                            // Exemplo: O sprite da caixa está no índice 6 da lista monstroSprites
                            spriteParaMostrar = tile.escondido ? monstroSprites[6] : monstroSprites[tile.corDoMonstro];
                            break;
                        case LevelData.TipoDeTile.Parede:
                            spriteParaMostrar = monstroSprites[7]; // Exemplo: sprite da parede
                            break;
                        case LevelData.TipoDeTile.Gerador:
                            spriteParaMostrar = monstroSprites[8]; // Exemplo: sprite do gerador
                            break;
                    }
                    pecaImage.sprite = spriteParaMostrar;

                    // Configura os dados do monstro
                    monstroComponent.posicaoGrid = new Tuple<int, int>(x, y);
                    monstroComponent.escondido = tile.escondido;
                    monstroComponent.cor = (tile.tipo == LevelData.TipoDeTile.Parede) ? -1 : tile.corDoMonstro;
                    monstroComponent.gridController = this;

                    if (tile.tipo == LevelData.TipoDeTile.Gerador && pecaTransform.TryGetComponent(out GeradorDeMonstros geradorComponent))
                    {
                        geradorComponent.monstrosParaGerar = tile.MonstrosASeremGeradosPeloGerador;
                    }

                    Monstros[x][y] = monstroComponent;
                }
            }
        }
    }

    public void ProcessarCliqueNoMonstro(Monstro monstroClicado)
    {
        if (monstroClicado == null || !monstroClicado.gameObject.activeInHierarchy || monstroClicado.vazio || monstroClicado.cor == -1) return;

        if (PodeRemover(monstroClicado) && Armazem.Count < maxArmazem)
        {
            AdicionarAoArmazem(monstroClicado);
            RemoverDoGrid(monstroClicado);
            VerificarVitoriaDoEstagio();
        }
    }

    void AdicionarAoArmazem(Monstro monstro)
    {
        if (!Armazem.Contains(monstro))
        {
            Armazem.Add(monstro);
            
        }
        Armazem.Sort((a, b) => a.cor.CompareTo(b.cor));
        AtualizarUIArmazem();
        ChecarEEliminarGrupos();

        if (Armazem.Count >= maxArmazem)
        {
            Derrota("Bandeja Cheia!");
        }
    }

    void RemoverDoGrid(Monstro monstro)
    {
        Monstros[monstro.posicaoGrid.Item1][monstro.posicaoGrid.Item2] = null;
        monstro.gameObject.SetActive(false);
    }

    void VerificarVitoriaDoEstagio()
    {
        bool monstrosRestantesNoGrid = Monstros.Any(coluna => coluna.Any(m => m != null && m.cor != -1));
        if (monstrosRestantesNoGrid) return;

        bool bandejaVazia = Armazem.Count == 0;
        if (bandejaVazia)
        {
            estagioAtual++;
            if (estagioAtual >= levelGroupAtual.estagios.Count)
            {
                Vitoria("Nível Concluído!");
            }
            else
            {
                StartCoroutine(CarregarProximoEstagioComDelay(1.5f));
            }
            return;
        }

        if (!bandejaVazia)
        {
            var contagemDeCores = Armazem.GroupBy(m => m.cor).ToDictionary(g => g.Key, g => g.Count());
            bool combinacaoPossivel = contagemDeCores.Any(par => par.Value >= 3);
            if (!combinacaoPossivel)
            {
                Derrota("Sem combinações possíveis!");
            }
        return; // Sai da função após iniciar a transição
    }

        // --- CONDIÇÃO DE DERROTA POR "SOFT-LOCK" ---
        // Se o grid está vazio, mas a bandeja NÃO está, verificamos se ainda é possível fazer combinações.
        if (!bandejaVazia)
        {
            // Agrupa os monstros na bandeja por cor e conta quantos há de cada um.
            var contagemDeCores = Armazem.GroupBy(m => m.cor)
                                         .ToDictionary(g => g.Key, g => g.Count());

            // Verifica se existe algum grupo de cor com 3 ou mais monstros.
            bool combinacaoPossivel = contagemDeCores.Any(par => par.Value >= 3);

            // Se NENHUMA combinação for possível, o jogador está preso. É uma derrota.
            if (!combinacaoPossivel)
            {
                Derrota("Sem combinações possíveis!");
            }
        }
    }

    IEnumerator CarregarProximoEstagioComDelay(float delay)
    {
        // Trava o jogo durante a transição para evitar cliques indesejados
        jogoTerminou = true;

        yield return new WaitForSeconds(delay); // Espera pelo tempo definido

        // Destrava o jogo e inicia o próximo estágio
        jogoTerminou = false;
        StartCoroutine(InicializarEstagioAposLayout(estagioAtual));
    }

    void Vitoria(string mensagem)
    {
        if (jogoTerminou) return;
        jogoTerminou = true;
        Debug.Log("VITÓRIA: " + mensagem);
        if (painelVitoria != null) painelVitoria.SetActive(true);
    }

    void Derrota(string mensagem)
    {
        if (jogoTerminou) return;
        jogoTerminou = true;
        Debug.Log("DERROTA: " + mensagem);
        if (painelDerrota != null) painelDerrota.SetActive(true);
    }

    void ChecarEEliminarGrupos()
    {
        var gruposParaRemover = Armazem.GroupBy(m => m.cor)
                                     .Where(g => g.Count() >= 3)
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
        return true;
    }

    private bool TemCaminhoLivre(int x, int y)
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

            if (vizinhoX >= 0 && vizinhoX < gridWidth && vizinhoY >= 0 && vizinhoY < gridHeight)
            {
                if (Monstros[vizinhoX][vizinhoY] == null)
                {
                    if (BuscaPorCaminhoAteBorda(vizinhoX, vizinhoY))
                    {
                        return true;
                    }
                }
            }
        }

        // Se não está na borda de baixo e não encontrou nenhum caminho, está presa.
        return false;
    }

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



    void AtualizarUIArmazem()
    {
        if (armazemUIParent == null) return;
        foreach (Transform child in armazemUIParent)
            Destroy(child.gameObject);

        if (monstroIconPrefab == null) return;
        foreach (Monstro monstro in Armazem)
        {
            GameObject monstroIcon = Instantiate(monstroIconPrefab, armazemUIParent);
            Image imageComponent = monstroIcon.GetComponent<Image>();
            if (imageComponent != null && monstro.cor >= 0 && monstro.cor < monstroSprites.Count)
            {
                imageComponent.sprite = monstroSprites[monstro.cor];
            }
        }
    }

#if UNITY_EDITOR
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