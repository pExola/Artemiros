using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridController : MonoBehaviour
{
    [Header("Configuração do Nível")]
    public LevelData nivelAtual;

    [Header("Configuração do Grid")]
    private int gridWidth;
    private int gridHeight;

    [Header("Prefabs")]
    public GameObject gridCellPrefab; // Arraste aqui o Prefab da célula de fundo
    public GameObject monstroPrefab;
    public GameObject paredePrefab;

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
                    // Instancia a célula de fundo na posição correta
                    GameObject cell = Instantiate(gridCellPrefab, new Vector3(x, y, 1), Quaternion.identity, this.transform);
                    // Define o nome da célula para facilitar a identificação
                    cell.name = $"Grid Cell ({x}, {y})";
                }

                // 2. Popula com monstros e paredes do LevelData
                // Mapeia a linha do inspector (de cima para baixo) para a linha do grid (de baixo para cima)
                int linhaDoLayout = (gridHeight - 1) - y;
                // Verifica se a linha existe no layout do grid
                LevelData.TileConfig tile = nivelAtual.layoutDoGrid[linhaDoLayout].colunas[x];
                GameObject prefabParaInstanciar = null;
                // Determina qual prefab instanciar com base no tipo de tile
                switch (tile.tipo)
                {
                    case LevelData.TipoDeTile.Monstro:
                        prefabParaInstanciar = monstroPrefab;
                        break;
                    case LevelData.TipoDeTile.Parede:
                        prefabParaInstanciar = paredePrefab;
                        break;
                }
                // Se o prefab não for nulo, instancia o monstro ou parede
                if (prefabParaInstanciar != null)
                {
                    // Instancia o prefab na posição correta
                    GameObject obj = Instantiate(prefabParaInstanciar, new Vector3(x, y, 0), Quaternion.identity, this.transform);
                    // Define o nome do objeto para facilitar a identificação
                    obj.name = $"{tile.tipo} ({x}, {y})";
                    // Adiciona o componente Monstro ao objeto
                    Monstro monstroComponent = obj.GetComponent<Monstro>();
                    
                    monstroComponent.posicaoGrid = new Tuple<int, int>(x, y);
                    monstroComponent.vazio = (tile.tipo == LevelData.TipoDeTile.Vazio);

                    if(tile.tipo == LevelData.TipoDeTile.Monstro)
                    {
                        monstroComponent.cor = tile.corDoMonstro;
                    }
                    if(tile.tipo == LevelData.TipoDeTile.Parede)
                    {
                        monstroComponent.cor = -1; // Definindo cor como -1 para paredes
                    }

                    Monstros[x][y] = monstroComponent;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null)
            {
                Debug.Log($"Clicou no objeto: {hit.collider.name}");
                Monstro monstroClicado = hit.collider.GetComponent<Monstro>();
                Debug.Log($"Clicou no monstro: {monstroClicado?.name}");
                if (monstroClicado == null) return;
                if (monstroClicado.vazio) return; // Ignora células vazias
                if (monstroClicado.cor == -1) return; // Ignora paredes
                // Usa a referência da parte principal para garantir que estamos movendo o monstro certo
                Monstro monstroPrincipal = monstroClicado;
                bool podeRemover = PodeRemover(monstroPrincipal);
                Debug.Log($"Pode remover: {podeRemover}");
                if ( podeRemover && Armazem.Count < maxArmazem)
                {
                    AdicionarAoArmazem(monstroPrincipal);
                    RemoverDoGrid(monstroPrincipal);
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
