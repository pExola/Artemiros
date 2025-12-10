using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipalManager : MonoBehaviour
{
    [Header("Ferramentas de Teste (DEV ONLY)")]
    [Tooltip("Se marcado, apaga todo o progresso sempre que o jogo inicia.")]
    public bool resetarProgressoAoIniciar = false; // Deixe TRUE para testar, FALSE para jogar normal

    [Header("Nome da Cena de Seleção de Fases")]
    public string nomeCenaFases = "SelecaoFases";

    void Awake()
    {
        // Verifica se a opção de resetar está marcada no Inspector
        if (resetarProgressoAoIniciar)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.LogWarning("⚠️ ATENÇÃO: Progresso resetado pelo Modo de Teste!");
        }
    }

    public void BotaoPlay()
    {
        SceneManager.LoadScene(nomeCenaFases);
    }

    public void BotaoOptions()
    {
        Debug.Log("Abrir Opções (A implementar)");
    }

    public void BotaoQuit()
    {
        Debug.Log("Saindo do Jogo...");
        Application.Quit();
    }
}