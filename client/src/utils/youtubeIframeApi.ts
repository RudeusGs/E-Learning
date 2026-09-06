export interface YouTubePlayer {
  playVideo(): void
  pauseVideo(): void
  seekTo(seconds: number, allowSeekAhead?: boolean): void
  getCurrentTime(): number
  getPlayerState(): number
  getDuration(): number
  getVolume(): number
  isMuted(): boolean
  mute(): void
  unMute(): void
  setPlaybackRate(rate: number): void
  destroy(): void
}

interface YouTubeNamespace {
  Player: new (
    element: HTMLElement,
    options: {
      videoId: string
      host?: string
      playerVars?: Record<string, number | string>
      events?: {
        onReady?: () => void
        onStateChange?: (event: { data: number }) => void
        onPlaybackRateChange?: () => void
      }
    },
  ) => YouTubePlayer
  PlayerState: {
    ENDED: number
    PLAYING: number
    PAUSED: number
  }
}

declare global {
  interface Window {
    YT?: YouTubeNamespace
    onYouTubeIframeAPIReady?: () => void
  }
}

let loader: Promise<YouTubeNamespace> | null = null

export function loadYouTubeIframeApi(): Promise<YouTubeNamespace> {
  if (window.YT?.Player) return Promise.resolve(window.YT)
  if (loader) return loader

  loader = new Promise((resolve, reject) => {
    const previous = window.onYouTubeIframeAPIReady
    window.onYouTubeIframeAPIReady = () => {
      previous?.()
      if (window.YT) resolve(window.YT)
      else reject(new Error('YouTube IFrame API is unavailable.'))
    }

    if (!document.querySelector('script[data-elearning-youtube-api]')) {
      const script = document.createElement('script')
      script.src = 'https://www.youtube.com/iframe_api'
      script.async = true
      script.dataset.elearningYoutubeApi = 'true'
      script.onerror = () => reject(new Error('Unable to load YouTube IFrame API.'))
      document.head.appendChild(script)
    }
  })

  return loader
}
