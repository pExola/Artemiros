using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BotaoSelecao : MonoBehaviour
{
    [Header("Configuração de Navegação")]
    [Tooltip("Nome exato da cena que este botão vai carregar")]
    public string cenaAlvo;

    [Header("Configuração de Bloqueio")]
    [Tooltip("ID único para este nível/fase. Ex: Fase 1 = 1, Fase 2 = 2")]
    public int idDoNivel;

    [Tooltip("Se marcado, este botão sempre estará desbloqueado (ex: Fase 1)")]
    public bool desbloqueadoPorPadrao = false;

    [Header("Referências Visuais")]
    public Button meuBotao;
    public Image cadeadoIcone; 

    void Start()
    {
        VerificarBloqueio();
    }

    void VerificarBloqueio()
    {
        // Lógica: O nível está desbloqueado se:
        // 1. É desbloqueado por padrão (Fase 1) OU
        // 2. O valor salvo no PlayerPrefs "FaseDesbloqueada" é maior ou igual ao ID deste botão.

        // Exemplo: Se o jogador completou a Fase 1, salvamos "2" no PlayerPrefs.
        // Logo, o botão com ID 2 será desbloqueado.

        int progressoAtual = PlayerPrefs.GetInt("ProgressoFases", 1); 

        bool estaDesbloqueado = desbloqueadoPorPadrao || (progressoAtual >= idDoNivel);

        if (estaDesbloqueado)
        {
            meuBotao.interactable = true;
            if (cadeadoIcone != null) cadeadoIcone.enabled = false;
        }
        else
        {
            meuBotao.interactable = false; 
            if (cadeadoIcone != null) cadeadoIcone.enabled = true;

            // Deixar o botão cinza
            var colors = meuBotao.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            meuBotao.colors = colors;
        }
    }

    // Associe esta função ao OnClick do botão no Inspector
    public void CarregarCena()
    {
        if (!string.IsNullOrEmpty(cenaAlvo))
        {
            SceneManager.LoadScene(cenaAlvo);
        }
        else
        {
            Debug.LogError($"O botão {gameObject.name} não tem uma cena alvo definida!");
        }
    }
}