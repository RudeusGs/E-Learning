<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

import * as lessonApi from '@/modules/lessons/lesson.api'
import type { VideoProgressState } from '@/modules/lessons/models/VideoProgressState'
import { loadYouTubeIframeApi, type YouTubePlayer } from '@/utils/youtubeIframeApi'

interface Checkpoint {
  id: number
  timestampSeconds: number
  passed: boolean
}

const props = defineProps<{
  lessonId: number
  videoId: string
  durationSeconds: number
  initialMaxPositionSeconds: number
  completed: boolean
  checkpoints: Checkpoint[]
}>()

const emit = defineEmits<{
  progress: [value: VideoProgressState]
  checkpoint: [questionId: number]
  ended: []
}>()

const host = ref<HTMLElement | null>(null)
const player = ref<YouTubePlayer | null>(null)
const ready = ref(false)
const playing = ref(false)
const muted = ref(false)
const current = ref(0)
const trustedMax = ref(Math.max(0, props.initialMaxPositionSeconds))
const sending = ref(false)
const message = ref('')

let monitorTimer: number | null = null
let heartbeatTimer: number | null = null
let lastObserved = 0
let seekCorrection = false

const percent = computed(() =>
  props.durationSeconds > 0 ? Math.min(100, current.value * 100 / props.durationSeconds) : 0,
)

const trustedPercent = computed(() =>
  props.durationSeconds > 0
    ? Math.min(100, trustedMax.value * 100 / props.durationSeconds)
    : 0,
)

function formatTime(seconds: number): string {
  const value = Math.max(0, Math.floor(seconds))
  const minutes = Math.floor(value / 60)
  const rest = String(value % 60).padStart(2, '0')
  return `${minutes}:${rest}`
}

function nextCheckpointLimit(): number {
  const pending = [...props.checkpoints]
    .filter((item) => !item.passed)
    .sort((a, b) => a.timestampSeconds - b.timestampSeconds)[0]
  return pending?.timestampSeconds ?? props.durationSeconds
}

function pause(): void {
  player.value?.pauseVideo()
}

function togglePlay(): void {
  if (!player.value) return
  if (playing.value) pause()
  else player.value.playVideo()
}

function rewind(): void {
  if (!player.value) return
  player.value.seekTo(Math.max(0, player.value.getCurrentTime() - 10), true)
}

function toggleMute(): void {
  if (!player.value) return
  if (player.value.isMuted()) player.value.unMute()
  else player.value.mute()
  muted.value = player.value.isMuted()
}

function pauseWhenHidden(): void {
  if (document.hidden) pause()
}

function pauseOnBlur(): void {
  pause()
}

function monitor(): void {
  const instance = player.value
  if (!instance || !ready.value) return

  const position = Math.max(0, instance.getCurrentTime() || 0)
  current.value = position
  const forwardAllowance = Math.max(trustedMax.value + 3, lastObserved + 3)
  const checkpointLimit = nextCheckpointLimit()
  const allowed = Math.min(forwardAllowance, checkpointLimit)

  if (!seekCorrection && position > allowed + 1) {
    seekCorrection = true
    instance.pauseVideo()
    instance.seekTo(Math.max(0, Math.min(trustedMax.value, checkpointLimit)), true)
    message.value = 'Không thể tua tới phần chưa xem.'
    window.setTimeout(() => { seekCorrection = false }, 400)
    return
  }

  const due = [...props.checkpoints]
    .filter((item) => !item.passed && position >= item.timestampSeconds - 0.5)
    .sort((a, b) => a.timestampSeconds - b.timestampSeconds)[0]

  if (due) {
    instance.pauseVideo()
    void heartbeat()
  }

  lastObserved = position
}

async function heartbeat(final = false): Promise<void> {
  if (!player.value || sending.value) return
  sending.value = true
  try {
    const position = final ? props.durationSeconds : Math.floor(player.value.getCurrentTime() || 0)
    const result = await lessonApi.recordVideoHeartbeat(props.lessonId, position)
    trustedMax.value = Math.max(trustedMax.value, result.maxPositionSeconds)
    emit('progress', result)

    if (result.blockedByQuestionId) {
      player.value.pauseVideo()
      player.value.seekTo(Math.min(result.maxPositionSeconds, nextCheckpointLimit()), true)
      emit('checkpoint', result.blockedByQuestionId)
    }
  } catch {
    message.value = 'Chưa đồng bộ được tiến độ video. Hệ thống sẽ thử lại.'
  } finally {
    sending.value = false
  }
}

onMounted(async () => {
  if (!host.value) return
  const yt = await loadYouTubeIframeApi()
  if (!host.value) return

  player.value = new yt.Player(host.value, {
    videoId: props.videoId,
    host: 'https://www.youtube-nocookie.com',
    playerVars: {
      controls: 0,
      disablekb: 1,
      fs: 0,
      playsinline: 1,
      rel: 0,
      modestbranding: 1,
    },
    events: {
      onReady: () => {
        ready.value = true
        player.value?.setPlaybackRate(1)
        current.value = Math.min(trustedMax.value, props.durationSeconds)
        if (trustedMax.value > 0) player.value?.seekTo(trustedMax.value, false)
      },
      onStateChange: (event) => {
        const wasPlaying = playing.value
        playing.value = event.data === yt.PlayerState.PLAYING

        if (event.data === yt.PlayerState.PLAYING && !wasPlaying) {
          // Establish/reset the server wall-clock baseline when playback actually begins.
          void heartbeat()
        } else if (event.data === yt.PlayerState.PAUSED && wasPlaying) {
          // Persist the last legitimately played position before a pause/tab switch.
          void heartbeat()
        }

        if (event.data === yt.PlayerState.ENDED) {
          void heartbeat(true)
          emit('ended')
        }
      },
      onPlaybackRateChange: () => player.value?.setPlaybackRate(1),
    },
  })

  monitorTimer = window.setInterval(monitor, 500)
  heartbeatTimer = window.setInterval(() => {
    if (playing.value) void heartbeat()
  }, 30_000)
  document.addEventListener('visibilitychange', pauseWhenHidden)
  window.addEventListener('blur', pauseOnBlur)
})

onBeforeUnmount(() => {
  if (playing.value) void heartbeat()
  if (monitorTimer !== null) window.clearInterval(monitorTimer)
  if (heartbeatTimer !== null) window.clearInterval(heartbeatTimer)
  document.removeEventListener('visibilitychange', pauseWhenHidden)
  window.removeEventListener('blur', pauseOnBlur)
  player.value?.destroy()
})
</script>

<template>
  <div class="overflow-hidden rounded-2xl border border-[#3f3535] bg-[#191616] text-white shadow-[0_12px_32px_rgba(31,15,18,0.16)]">
    <div class="relative aspect-video bg-black">
      <div ref="host" class="absolute inset-0 size-full" />
      <button type="button" class="absolute inset-0 z-10 cursor-default" aria-label="Trình phát video được khóa tua" @click="togglePlay" />
      <div class="pointer-events-none absolute inset-x-0 bottom-0 z-20 h-24 bg-gradient-to-t from-black/75 to-transparent" />
      <div class="absolute inset-x-4 bottom-4 z-30 flex items-center gap-3">
        <button type="button" class="grid size-9 place-items-center rounded-full bg-white text-[#7f1020]" @click.stop="togglePlay">
          <svg v-if="!playing" class="size-4" viewBox="0 0 24 24" fill="currentColor"><path d="m9 7 8 5-8 5Z" /></svg>
          <svg v-else class="size-4" viewBox="0 0 24 24" fill="currentColor"><path d="M7 6h4v12H7zM13 6h4v12h-4z" /></svg>
        </button>
        <button type="button" class="rounded-lg bg-white/12 px-2.5 py-2 text-[11px] font-bold hover:bg-white/20" @click.stop="rewind">−10s</button>
        <div class="min-w-0 flex-1">
          <div class="relative h-1.5 overflow-hidden rounded-full bg-white/20">
            <div class="absolute inset-y-0 left-0 rounded-full bg-white/25" :style="{ width: `${percent}%` }" />
            <div class="absolute inset-y-0 left-0 rounded-full bg-[#ff6777]" :style="{ width: `${trustedPercent}%` }" />
          </div>
          <div class="mt-1.5 flex justify-between gap-3 text-[10px] font-semibold text-white/70">
            <span>{{ formatTime(current) }}</span>
            <span class="truncate">Đã ghi nhận {{ formatTime(trustedMax) }}</span>
            <span>{{ formatTime(durationSeconds) }}</span>
          </div>
        </div>
        <button type="button" class="rounded-lg bg-white/12 px-2.5 py-2 text-[11px] font-bold hover:bg-white/20" @click.stop="toggleMute">{{ muted ? 'Bật âm' : 'Tắt âm' }}</button>
      </div>
    </div>
    <div class="border-t border-white/10 bg-[#241e1f] px-4 py-3 text-[11px] leading-5 text-white/70">
      <div class="flex items-start gap-2">
        <svg class="mt-0.5 size-3.5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 3 4 7v5c0 5 3.4 8 8 9 4.6-1 8-4 8-9V7l-8-4Z" /></svg>
        <span>{{ message || 'Xem tuần tự để hệ thống ghi nhận tiến độ. Không thể tua tới phần chưa xem; có thể tua lùi để xem lại.' }}</span>
      </div>
      <div class="mt-2 flex flex-wrap gap-2 pl-5 text-[10px] font-bold text-white/55">
        <span class="rounded-md bg-white/[0.07] px-2 py-1">Tốc độ 1x</span>
        <span class="rounded-md bg-white/[0.07] px-2 py-1">Đổi tab → tự dừng</span>
        <span class="rounded-md bg-white/[0.07] px-2 py-1">Checkpoint → tự bật</span>
        <span class="rounded-md bg-white/[0.07] px-2 py-1">Đã ghi nhận {{ formatTime(trustedMax) }}</span>
      </div>
    </div>
  </div>
</template>