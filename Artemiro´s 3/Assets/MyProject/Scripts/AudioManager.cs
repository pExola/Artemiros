using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    // --- Padrão Singleton ---
    // Garante que só existe uma instância do AudioManager no jogo.
    public static AudioManager Instance;

    [Header("--------- Audio Sources ---------")]
    [Tooltip("Fonte de áudio para a música de fundo.")]
    public AudioSource musicSource;

    [Tooltip("Fonte de áudio para os efeitos sonoros (SFX).")]
    public AudioSource sfxSource;


    [Header("--------- Audio Clips ---------")]
    public AudioClip backgroundMusic;
    // Futuramente, você pode adicionar mais clips aqui:
    // public AudioClip clickSFX;
    // public AudioClip matchSFX;
    // public AudioClip winSFX;


    private void Awake()
    {
        // Implementação do Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Faz o AudioManager persistir entre as cenas
        }
        else
        {
            Destroy(gameObject); // Se outra instância for criada, destrói a nova
        }
    }

    private void Start()
    {
        // Toca a música de fundo assim que o jogo inicia
        PlayMusic(backgroundMusic);
    }

    /// <summary>
    /// Toca uma música de fundo de forma contínua (loop).
    /// </summary>
    /// <param name="musicClip">O clipe de música a ser tocado.</param>
    public void PlayMusic(AudioClip musicClip)
    {
        if (musicClip == null)
        {
            Debug.LogWarning("Nenhum clipe de música de fundo foi atribuído!");
            return;
        }

        musicSource.clip = musicClip;
        musicSource.Play();
    }

    /// <summary>
    /// Toca um efeito sonoro uma única vez.
    /// </summary>
    /// <param name="sfxClip">O clipe de SFX a ser tocado.</param>
    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("Nenhum clipe de SFX foi passado para o método PlaySFX!");
            return;
        }

        // Usa PlayOneShot para permitir que vários SFX toquem sobrepostos sem se cortar.
        sfxSource.PlayOneShot(sfxClip);
    }
}