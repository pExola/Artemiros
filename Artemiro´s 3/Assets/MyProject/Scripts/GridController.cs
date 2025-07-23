using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;

public class GridController : MonoBehaviour
{
    [Header("Configuração do Nível")]
    public LevelData nivelAtual;
    public float MaximoDeSegundos = 60; // Tempo máximo para completar o nível, em segundos
    public TMPro.TMP_Text tempoRestanteText; // Arraste aqui o componente TextMeshPro para mostrar o tempo restante

    [Header("Configuração do Grid")]
    private int gridWidth;
    private int gridHeight;

    [Header("Prefabs")]
    public GameObject gridCellPrefab; // Arraste aqui o Prefab da célula de fundo
    public List<GameObject> monstroPrefab;
    public GameObject paredePrefab;
    public GameObject prefabCaixa; // Prefab para monstros escondidos (caixas)
    public GameObject prefabGerador; // Prefab para o gerador de monstros
    [Header("UI do Armazém")]
    public Transform armazemUIParent; // Arraste o painel da UI para cá
    public GameObject monstroIconPrefab; // Arraste o prefab do ícone do monstro para cá
    public List<Sprite> monstroSprites; // Lista de sprites, um para cada cor de monstro

    [Header("Estado do Jogo")]
    public List<List<Monstro>> Monstros = new List<List<Monstro>>();
    public List<Monstro> Armazem = new List<Monstro>();
    private int maxArmazem = 7;

    // Use this for initialization
    void Start()
    {
        InicializarGrid();
    }

    void InicializarGrid()
    {
        if (nivelAtual == null || nivelAtual.layoutDoGrid == null)
        {
            Debug.LogError("Nenhum LevelData válido foi atribuído ao GridController!");
            return;
        }

        // Define a largura e altura do grid com base no layout do nível
        gridHeight = nivelAtual.layoutDoGrid.Count;
        gridWidth = (gridHeight > 0) ? nivelAtual.layoutDoGrid[0].colunas.Count : 0;
        maxArmazem = nivelAtual.TamanhoInventario;
        // Limpa o grid antigo, se houver
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        // Limpa o armazém
        Monstros = new List<List<Monstro>>();
        for (int x = 0; x < gridWidth; x++)
        {
            // Inicializa cada coluna com uma lista de monstros vazia
            Monstros.Add(new List<Monstro>(new Monstro[gridHeight]));
        }

        // A iteração agora é invertida para que o layout no Inspector (de cima para baixo)
        // corresponda ao grid no jogo (de baixo para cima).
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // 1. Desenha a célula de fundo
                if (gridCellPrefab != null)
                {
                    GameObject cell = Instantiate(gridCellPrefab, new Vector3(x, y, 1), Quaternion.identity, this.transform);
                    cell.name = $"Grid Cell ({x}, {y})";
                }

                // 2. Popula com monstros e paredes do LevelData
                int linhaDoLayout = (gridHeight - 1) - y;
                if (nivelAtual.layoutDoGrid.Count <= linhaDoLayout || nivelAtual.layoutDoGrid[linhaDoLayout].colunas == null || nivelAtual.layoutDoGrid[linhaDoLayout].colunas.Count <= x)
                    continue; // Evita NullReference
                LevelData.TileConfig tile = nivelAtual.layoutDoGrid[linhaDoLayout].colunas[x];
                GameObject prefabParaInstanciar = null;
                switch (tile.tipo)
                {
                    case LevelData.TipoDeTile.Monstro:
                        if (tile.escondido)
                        {
                            prefabParaInstanciar = prefabCaixa;
                            break;
                        }
                        if (monstroPrefab != null && tile.corDoMonstro >= 0 && tile.corDoMonstro < monstroPrefab.Count && monstroPrefab[tile.corDoMonstro] != null)
                        {
                            prefabParaInstanciar = monstroPrefab[tile.corDoMonstro];
                        }
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
                    GameObject obj = Instantiate(prefabParaInstanciar, new Vector3(x, y, 0), Quaternion.identity, this.transform);
                    obj.name = $"{tile.tipo} ({x}, {y})";
                    if (tile.tipo == LevelData.TipoDeTile.Gerador)
                    {
                        GeradorDeMonstros geradorComponent = obj.GetComponent<GeradorDeMonstros>();
                        if (geradorComponent != null)
                        {
                            geradorComponent.monstrosParaGerar = tile.MonstrosASeremGeradosPeloGerador;
                        }
                    }
                    Monstro monstroComponent = obj.GetComponent<Monstro>();
                    if (monstroComponent != null)
                    {
                        monstroComponent.posicaoGrid = new Tuple<int, int>(x, y);
                        monstroComponent.vazio = (tile.tipo == LevelData.TipoDeTile.Vazio);
                        monstroComponent.escondido = tile.escondido;
                        if(tile.tipo == LevelData.TipoDeTile.Monstro)
                        {
                            monstroComponent.cor = tile.corDoMonstro;
                        }
                        if(tile.tipo == LevelData.TipoDeTile.Parede)
                        {
                            monstroComponent.cor = -1;
                        }
                        Monstros[x][y] = monstroComponent;
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Atualiza o tempo restante


        if (MaximoDeSegundos > 0 && tempoRestanteText != null)
        {
            MaximoDeSegundos -= Time.deltaTime;
            if (MaximoDeSegundos < 0) MaximoDeSegundos = 0;
            tempoRestanteText.text = $"{MaximoDeSegundos.ToString("F2")}";
        }
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null)
            {
                Debug.Log($"Clicou no objeto: {hit.collider.name}");
                Monstro monstroClicado = hit.collider.GetComponent<Monstro>();
                GameObject objetoClicado = hit.collider.gameObject;
                Debug.Log($"Clicou no monstro: {monstroClicado?.name}");
                if (monstroClicado == null) return;
                if (monstroClicado.vazio) return; // Ignora células vazias
                if (monstroClicado.cor == -1) return; // Ignora paredes
                
                // Usa a referência da parte principal para garantir que estamos movendo o monstro certo
                bool podeRemover = PodeRemover(monstroClicado);
                Debug.Log($"Pode remover: {podeRemover}");
                if ( podeRemover && Armazem.Count < maxArmazem)
                {

                    AdicionarAoArmazem(monstroClicado);
                    RemoverDoGrid(monstroClicado);
                }
            }
        }

    }
    
    // ... (restante do código: PodeRemover, RemoverDoGrid, AdicionarAoArmazem, ChecarEEliminarGrupos) ...
    // O código abaixo permanece o mesmo, mas o refatorei para clareza e para usar a nova estrutura do Monstro.cs

    bool PodeRemover(Monstro monstro)
    {
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
}
