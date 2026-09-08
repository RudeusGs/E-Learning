<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'

import { toProblem, type ProblemDetails } from '@/api/http'
import GuardedYouTubePlayer from '@/components/GuardedYouTubePlayer.vue'
import ProblemAlert from '@/components/ProblemAlert.vue'
import * as courseApi from '@/modules/courses/course.api'
import type { StudentCourseDetail } from '@/modules/courses/models/StudentCourseDetail'
import type { AnswerResult } from '@/modules/exercises/models/AnswerResult'
import ExerciseCard from '@/modules/exercises/ExerciseCard.vue'

import * as lessonApi from './lesson.api'
import { hasPassedReinforcement } from './learningProgress'
import type { StudentLesson } from './models/StudentLesson'

import type { VideoProgressState } from './models/VideoProgressState'

const route = useRoute()
const lessonId = computed(() => Number(route.params.lessonId))
const lesson = ref<StudentLesson | null>(null)
const course = ref<StudentCourseDetail | null>(null)
const loading = ref(true)
const completing = ref(false)
const problem = ref<ProblemDetails | null>(null)
const completionMessage = ref('')
const pathOpen = ref(false)
const activeCheckpointId = ref<number | null>(null)

const isCompleted = computed(() => lesson.value?.progressStatus === 'COMPLETED')
const lockedProblem = computed(() => problem.value?.code === 'LESSON_LOCKED')
const videoDuration = computed(() => lesson.value?.videoProgress.durationSeconds ?? null)
const reinforcementThresholdMet = computed(() => {
  const completion = lesson.value?.completion
  if (!completion) return false
  return hasPassedReinforcement(
    completion.reinforcementPassed,
    completion.reinforcementTotal,
    completion.requiredScorePercent,
  )
})
const reinforcementScoreLabel = computed(() => {
  const completion = lesson.value?.completion
  if (!completion || completion.reinforcementTotal === 0) return '100'
  const exact = (completion.reinforcementPassed * 100) / completion.reinforcementTotal
  return Number.isInteger(exact) ? String(exact) : exact.toFixed(1)
})
const checkpointQuestions = computed(
  () => lesson.value?.questions.filter((q) => q.placement === 'VIDEO_CHECKPOINT') ?? [],
)
const reinforcementQuestions = computed(
  () => lesson.value?.questions.filter((q) => q.placement === 'REINFORCEMENT') ?? [],
)
const checkpointSchedule = computed(() =>
  checkpointQuestions.value
    .filter((q) => q.videoTimestampSeconds !== null)
    .map((q) => ({ id: q.id, timestampSeconds: q.videoTimestampSeconds ?? 0, passed: q.passed })),
)
const activeCheckpoint = computed(
  () => checkpointQuestions.value.find((q) => q.id === activeCheckpointId.value) ?? null,
)
const currentLessonIndex = computed(() =>
  course.value && lesson.value
    ? course.value.lessons.findIndex((item) => item.id === lesson.value?.id)
    : -1,
)

const nextRequirement = computed(() => {
  const current = lesson.value
  if (!current) return ''

  if (current.progressStatus === 'COMPLETED') {
    return 'Bài học đã hoàn thành. Bạn có thể sang bài tiếp theo hoặc học lại.'
  }

  if (current.completion.videoRequired && !current.completion.videoConfigured) {
    return 'Video chưa được giáo viên cấu hình thời lượng. Bạn chưa thể hoàn thành bài này.'
  }

  if (current.completion.videoRequired && !current.completion.videoCompleted) {
    return 'Xem video theo thứ tự đến hết. Checkpoint sẽ tự xuất hiện đúng mốc và video sẽ dừng để bạn trả lời.'
  }

  if (current.completion.checkpointPassed < current.completion.checkpointTotal) {
    return `Còn ${current.completion.checkpointTotal - current.completion.checkpointPassed} checkpoint cần trả lời đúng.`
  }

  if (!reinforcementThresholdMet.value) {
    return `Làm bài củng cố và đạt trên ${current.completion.requiredScorePercent}%. Hiện tại: ${reinforcementScoreLabel.value}%.`
  }

  return 'Bạn đã đủ điều kiện. Bấm “Hoàn thành bài học” để lưu kết quả và mở bài tiếp theo.'
})

const learningSteps = computed(() => {
  const current = lesson.value
  if (!current) return []

  return [
    {
      label: 'Xem nội dung',
      detail: current.completion.videoRequired ? 'Xem video theo thứ tự' : 'Đọc nội dung bài học',
      done: !current.completion.videoRequired || current.completion.videoCompleted,
      target: 'lesson-video',
    },
    {
      label: 'Checkpoint',
      detail:
        current.completion.checkpointTotal > 0
          ? `${current.completion.checkpointPassed}/${current.completion.checkpointTotal} đã đúng`
          : 'Không có checkpoint',
      done: current.completion.checkpointPassed === current.completion.checkpointTotal,
      target: 'lesson-video',
    },
    {
      label: 'Bài củng cố',
      detail:
        current.completion.reinforcementTotal > 0
          ? `${reinforcementScoreLabel.value}% · cần > ${current.completion.requiredScorePercent}%`
          : 'Không có bài củng cố',
      done: reinforcementThresholdMet.value,
      target: 'reinforcement-section',
    },
    {
      label: 'Hoàn thành',
      detail: current.completion.canComplete ? 'Sẵn sàng xác nhận' : 'Chưa đủ điều kiện',
      done: current.progressStatus === 'COMPLETED',
      target: 'completion-section',
    },
  ]
})

function progressWidth(value: number): string {
  return `${Math.max(0, Math.min(100, value))}%`
}

function lessonNumber(index: number): string {
  return `Bài ${index + 1}`
}

function scrollToSection(id: string): void {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

function recalculateCompletion(): void {
  if (!lesson.value) return
  const checkpoints = checkpointQuestions.value
  const reinforcement = reinforcementQuestions.value
  const checkpointPassed = checkpoints.filter((q) => q.passed).length
  const reinforcementPassed = reinforcement.filter((q) => q.passed).length
  const score =
    reinforcement.length === 0
      ? 100
      : Math.round((reinforcementPassed * 100) / reinforcement.length)
  const videoRequired = Boolean(lesson.value.video)
  const videoCompleted = !videoRequired || lesson.value.videoProgress.completed
  const quizPassed = hasPassedReinforcement(
    reinforcementPassed,
    reinforcement.length,
    lesson.value.completion.requiredScorePercent,
  )

  lesson.value.completion = {
    ...lesson.value.completion,
    videoCompleted,
    reinforcementUnlocked: videoCompleted,
    checkpointTotal: checkpoints.length,
    checkpointPassed,
    reinforcementTotal: reinforcement.length,
    reinforcementPassed,
    reinforcementScorePercent: score,
    canComplete:
      isCompleted.value ||
      (lesson.value.completion.videoConfigured &&
        videoCompleted &&
        checkpointPassed === checkpoints.length &&
        quizPassed),
  }
}

async function load(): Promise<void> {
  const requested = lessonId.value
  if (!Number.isFinite(requested) || requested <= 0) {
    problem.value = {
      status: 400,
      code: 'INVALID_LESSON_ID',
      title: 'Bài học không hợp lệ',
      detail: 'Mã bài học không hợp lệ.',
    }
    loading.value = false
    return
  }

  loading.value = true
  lesson.value = null
  course.value = null
  problem.value = null
  completionMessage.value = ''
  activeCheckpointId.value = null

  try {
    const loaded = await lessonApi.getStudentLesson(requested)
    if (lessonId.value !== requested) return
    lesson.value = loaded
    course.value = await courseApi.getStudentCourse(loaded.courseId)
    if (lessonId.value !== requested) return

    if (loaded.progressStatus === 'NOT_STARTED') {
      const progress = await lessonApi.startLesson(requested)
      if (lesson.value?.id === requested) lesson.value.progressStatus = progress.status
    }
  } catch (error) {
    if (lessonId.value === requested) problem.value = toProblem(error)
  } finally {
    if (lessonId.value === requested) loading.value = false
  }
}

function onVideoProgress(value: VideoProgressState): void {
  if (!lesson.value) return
  lesson.value.videoProgress = value
  if (value.blockedByQuestionId) activeCheckpointId.value = value.blockedByQuestionId
  recalculateCompletion()
}

function openCheckpoint(questionId: number): void {
  const question = checkpointQuestions.value.find((item) => item.id === questionId)
  if (question && !question.passed) activeCheckpointId.value = questionId
}

function onAnswered(questionId: number, result: AnswerResult): void {
  if (!lesson.value) return
  const question = lesson.value.questions.find((item) => item.id === questionId)
  if (!question) return

  if (question.placement === 'VIDEO_CHECKPOINT') {
    if (result.correct) question.passed = true
  } else {
    // Reinforcement score reflects the learner's latest submitted answer.
    question.passed = result.correct
  }

  recalculateCompletion()
  if (activeCheckpointId.value === questionId && result.correct) {
    activeCheckpointId.value = null
    completionMessage.value = 'Checkpoint đã đúng. Bạn có thể tiếp tục video.'
  }
}

async function complete(): Promise<void> {
  if (
    !lesson.value ||
    completing.value ||
    isCompleted.value ||
    !lesson.value.completion.canComplete
  )
    return
  completing.value = true
  problem.value = null
  try {
    const result = await lessonApi.completeLesson(lesson.value.id)
    lesson.value.progressStatus = 'COMPLETED'
    lesson.value.nextLessonId = result.nextLessonId
    lesson.value.completion.canComplete = true
    completionMessage.value = result.nextLessonId
      ? 'Bài học đã hoàn thành. Bài tiếp theo đã được mở khóa.'
      : 'Bạn đã hoàn thành bài học cuối trong lộ trình hiện tại.'
    course.value = await courseApi.getStudentCourse(lesson.value.courseId)
  } catch (error) {
    problem.value = toProblem(error)
  } finally {
    completing.value = false
  }
}

onMounted(load)
watch(
  () => route.params.lessonId,
  () => void load(),
)
watch(activeCheckpointId, (value) => {
  document.body.style.overflow = value === null ? '' : 'hidden'
})
onBeforeUnmount(() => {
  document.body.style.overflow = ''
})
</script>

<template>
  <section class="-mx-4 -my-7 min-h-[calc(100vh-64px)] sm:-mx-6 sm:-my-10">
    <div v-if="loading" class="grid min-h-[calc(100vh-64px)] lg:grid-cols-[270px_minmax(0,1fr)]">
      <aside class="hidden border-r border-[#e5dedd] bg-white p-5 lg:block">
        <div class="h-24 animate-pulse rounded-xl bg-[#eee8e7]" />
        <div class="mt-5 h-[520px] animate-pulse rounded-xl bg-[#f1eceb]" />
      </aside>
      <main class="p-4 sm:p-6">
        <div class="mx-auto max-w-5xl space-y-5">
          <div class="aspect-video animate-pulse rounded-2xl bg-[#ded9d8]" />
          <div class="h-64 animate-pulse rounded-2xl bg-white" />
        </div>
      </main>
    </div>

    <div
      v-else-if="lockedProblem"
      class="mx-auto flex min-h-[calc(100vh-64px)] max-w-3xl items-center px-4 py-12"
    >
      <div class="w-full rounded-2xl border border-[#e8c9a7] bg-[#fff8ed] p-6">
        <h1 class="text-xl font-black text-[#4d3b26]">Bài học chưa mở khóa</h1>
        <p class="mt-2 text-sm leading-6 text-[#7a592c]">
          Hoàn thành bài Published đứng ngay trước để tiếp tục.
        </p>
        <ProblemAlert class="mt-4" :problem="problem" />
        <RouterLink
          to="/student/courses"
          class="mt-4 inline-flex h-9 items-center rounded-lg border border-[#d8b57f] bg-white px-3 text-xs font-bold text-[#8c5b16]"
          >Về khóa học của tôi</RouterLink
        >
      </div>
    </div>

    <div v-else-if="problem && !lesson" class="mx-auto max-w-4xl px-4 py-10">
      <ProblemAlert :problem="problem" />
    </div>

    <div
      v-else-if="lesson && course"
      class="grid min-h-[calc(100vh-64px)] bg-[#fcf9f8] lg:grid-cols-[270px_minmax(0,1fr)]"
    >
      <!-- Compact independent learning path: avoids making the whole admin/student page endlessly tall. -->
      <aside
        class="hidden border-r border-[#e5dedd] bg-white lg:sticky lg:top-16 lg:block lg:h-[calc(100vh-64px)] lg:overflow-y-auto"
      >
        <div class="sticky top-0 z-10 border-b border-[#eee7e6] bg-white/95 p-4 backdrop-blur">
          <RouterLink
            :to="`/student/courses/${course.id}`"
            class="text-[10px] font-black uppercase tracking-[0.12em] text-[#a0001c]"
            >← Lộ trình khóa học</RouterLink
          >
          <h2 class="mt-2 line-clamp-2 text-sm font-black leading-5 text-[#302929]">
            {{ course.title }}
          </h2>
          <div class="mt-3 flex items-center gap-2">
            <div class="h-1.5 flex-1 overflow-hidden rounded-full bg-[#eee8e7]">
              <div
                class="h-full bg-[#c9152b]"
                :style="{ width: progressWidth(course.percentage) }"
              />
            </div>
            <b class="text-[10px]">{{ course.percentage }}%</b>
          </div>
        </div>
        <nav class="p-4">
          <ol class="space-y-1.5">
            <li v-for="(item, index) in course.lessons" :key="item.id">
              <RouterLink
                v-if="item.canAccess"
                :to="`/student/learn/${item.id}`"
                class="flex gap-2 rounded-lg px-2.5 py-2 text-[11px] leading-4"
                :class="
                  item.id === lesson.id
                    ? 'bg-[#fff1f2] text-[#a0001c]'
                    : 'text-[#665957] hover:bg-[#faf7f6]'
                "
              >
                <span
                  class="grid size-5 shrink-0 place-items-center rounded-full text-[9px] font-black"
                  :class="
                    item.state === 'COMPLETED'
                      ? 'bg-[#e4f5ec] text-[#16764f]'
                      : item.id === lesson.id
                        ? 'bg-[#c9152b] text-white'
                        : 'bg-[#f0eceb]'
                  "
                  >{{ item.state === 'COMPLETED' ? '✓' : index + 1 }}</span
                >
                <span class="line-clamp-2 font-semibold">{{ item.title }}</span>
              </RouterLink>
              <div v-else class="flex gap-2 px-2.5 py-2 text-[11px] text-[#a09390]">
                <span class="grid size-5 shrink-0 place-items-center rounded-full bg-[#f1eeed]"
                  >🔒</span
                ><span class="line-clamp-2">{{ item.title }}</span>
              </div>
            </li>
          </ol>
        </nav>
      </aside>

      <main class="min-w-0 p-4 sm:p-5 lg:p-6">
        <div class="mx-auto max-w-5xl pb-12">
          <button
            type="button"
            class="mb-4 flex w-full items-center justify-between rounded-xl border border-[#e5dedd] bg-white px-4 py-3 text-left lg:hidden"
            @click="pathOpen = !pathOpen"
          >
            <span class="text-xs font-black text-[#403735]"
              >{{ lessonNumber(currentLessonIndex) }} · {{ course.title }}</span
            ><span class="text-xs font-bold text-[#a0001c]">{{ course.percentage }}%</span>
          </button>
          <div
            v-if="pathOpen"
            class="mb-4 max-h-64 overflow-y-auto rounded-xl border border-[#e5dedd] bg-white p-3 lg:hidden"
          >
            <RouterLink
              v-for="(item, index) in course.lessons"
              :key="item.id"
              :to="item.canAccess ? `/student/learn/${item.id}` : route.fullPath"
              class="block rounded-lg px-3 py-2 text-xs"
              :class="item.id === lesson.id ? 'bg-[#fff1f2] text-[#a0001c]' : 'text-[#665957]'"
              >{{ index + 1 }}. {{ item.title }}</RouterLink
            >
          </div>

          <ProblemAlert v-if="problem" class="mb-4" :problem="problem" />
          <div
            v-if="completionMessage"
            class="mb-4 rounded-xl border border-[#b9dfcc] bg-[#eff9f4] px-4 py-3 text-xs font-bold text-[#245b43]"
          >
            {{ completionMessage }}
          </div>

          <header class="mb-5">
            <p class="text-[10px] font-black uppercase tracking-[0.14em] text-[#a0001c]">
              {{ currentLessonIndex >= 0 ? lessonNumber(currentLessonIndex) : 'Bài học' }}
            </p>
            <h1
              class="mt-1.5 text-[25px] font-black leading-tight tracking-[-0.025em] text-[#1c1b1b] sm:text-[30px]"
            >
              {{ lesson.title }}
            </h1>
            <p v-if="lesson.description" class="mt-2 max-w-4xl text-sm leading-6 text-[#6b5d5c]">
              {{ lesson.description }}
            </p>
          </header>

          <section
            class="mb-5 rounded-2xl border border-[#e5dedd] bg-white p-4 shadow-[0_2px_10px_rgba(50,25,25,0.025)] sm:p-5"
            aria-label="Các bước để hoàn thành bài học"
          >
            <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
              <div>
                <p class="text-[10px] font-black uppercase tracking-[0.15em] text-[#a0001c]">
                  Bạn cần làm gì?
                </p>
                <h2 class="mt-1 text-sm font-black text-[#302929]">4 bước để hoàn thành bài học</h2>
              </div>
              <p
                class="max-w-xl rounded-lg border px-3 py-2 text-[11px] font-semibold leading-5"
                :class="
                  lesson.completion.canComplete || isCompleted
                    ? 'border-[#b9dfcc] bg-[#f0faf5] text-[#245b43]'
                    : 'border-[#efd1a7] bg-[#fff8ed] text-[#7b5521]'
                "
              >
                {{ nextRequirement }}
              </p>
            </div>

            <div class="mt-4 grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
              <button
                v-for="(step, index) in learningSteps"
                :key="step.label"
                type="button"
                class="flex items-start gap-2.5 rounded-xl border p-3 text-left transition hover:-translate-y-px"
                :class="
                  step.done
                    ? 'border-[#cfe4d8] bg-[#f4faf7]'
                    : 'border-[#e5dedd] bg-[#faf8f7] hover:border-[#d7a8ad] hover:bg-[#fffafa]'
                "
                @click="scrollToSection(step.target)"
              >
                <span
                  class="grid size-6 shrink-0 place-items-center rounded-full text-[10px] font-black"
                  :class="step.done ? 'bg-[#16875b] text-white' : 'bg-[#fff0f1] text-[#a0001c]'"
                >
                  {{ step.done ? '✓' : index + 1 }}
                </span>
                <span class="min-w-0">
                  <strong class="block text-[11px] text-[#403735]">{{ step.label }}</strong>
                  <span class="mt-1 block text-[10px] leading-4 text-[#81716f]">{{
                    step.detail
                  }}</span>
                </span>
              </button>
            </div>
          </section>

          <div id="lesson-video" class="scroll-mt-24">
            <GuardedYouTubePlayer
              v-if="lesson.video && videoDuration"
              :key="lesson.id"
              :lesson-id="lesson.id"
              :video-id="lesson.video.externalId"
              :duration-seconds="videoDuration ?? 0"
              :initial-max-position-seconds="lesson.videoProgress.maxPositionSeconds"
              :completed="lesson.videoProgress.completed"
              :checkpoints="checkpointSchedule"
              @progress="onVideoProgress"
              @checkpoint="openCheckpoint"
            />
            <div
              v-else-if="lesson.video"
              class="rounded-xl border border-[#efcfaa] bg-[#fff8ed] p-4 text-xs leading-5 text-[#7a592c]"
            >
              <b>Video chưa có thời lượng tin cậy.</b> Giáo viên cần nhập thời lượng video trong
              trang chỉnh sửa bài học trước khi học viên có thể hoàn thành bài này.
            </div>
          </div>

          <article class="mt-5 rounded-xl border border-[#e5dedd] bg-white p-4 sm:p-6">
            <h2 class="text-base font-black text-[#302929]">Nội dung bài học</h2>
            <!-- Backend sanitizer remains the source of trust for lesson HTML. -->
            <!-- eslint-disable vue/no-v-html -->
            <div
              v-if="lesson.contentHtml"
              class="lesson-content mt-4 text-sm leading-7 text-[#3f3736]"
              v-html="lesson.contentHtml"
            />
            <!-- eslint-enable vue/no-v-html -->
            <p v-else class="mt-4 text-sm text-[#81716f]">
              Bài học chưa có nội dung văn bản bổ sung.
            </p>
          </article>

          <section id="reinforcement-section" class="mt-6 scroll-mt-24">
            <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-end">
              <div>
                <p class="text-[10px] font-black uppercase tracking-[0.14em] text-[#a0001c]">
                  Củng cố
                </p>
                <h2 class="mt-1 text-xl font-black text-[#302929]">Bài tập cuối bài</h2>
                <p class="mt-1 text-xs leading-5 text-[#81716f]">
                  Chỉ mở sau khi xem hết video. Cần đúng trên
                  {{ lesson.completion.requiredScorePercent }}% số câu.
                </p>
              </div>
              <span
                class="text-xs font-black"
                :class="reinforcementThresholdMet ? 'text-[#16764f]' : 'text-[#a0001c]'"
                >{{ lesson.completion.reinforcementPassed }}/{{
                  lesson.completion.reinforcementTotal
                }}
                đúng · {{ reinforcementScoreLabel }}%</span
              >
            </div>
            <div v-if="reinforcementQuestions.length" class="mt-4 grid gap-4 xl:grid-cols-2">
              <ExerciseCard
                v-for="(question, index) in reinforcementQuestions"
                :key="question.id"
                :question="question"
                :index="index"
                :disabled="!lesson.completion.reinforcementUnlocked"
                locked-reason="Xem video đến hết trước khi làm bài củng cố."
                @answered="onAnswered"
              />
            </div>
            <div
              v-else
              class="mt-4 rounded-xl border border-[#e4dcdb] bg-white px-4 py-4 text-xs text-[#756765]"
            >
              Bài học này chưa có câu hỏi củng cố. Điều kiện điểm được coi là đạt; các điều kiện
              video/checkpoint vẫn áp dụng.
            </div>
          </section>

          <section
            id="completion-section"
            class="mt-6 scroll-mt-24 rounded-xl border border-[#e5dedd] bg-white p-4 sm:p-5"
          >
            <div class="flex flex-col justify-between gap-5 sm:flex-row sm:items-center">
              <div>
                <h2 class="text-sm font-black text-[#302929]">Điều kiện hoàn thành bài</h2>
                <div class="mt-3 flex flex-wrap gap-2 text-[10px] font-bold">
                  <span
                    class="rounded-lg border px-2.5 py-1.5"
                    :class="
                      lesson.completion.videoCompleted
                        ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
                        : 'border-[#e1d9d8] bg-[#f7f4f3] text-[#756765]'
                    "
                    >{{
                      lesson.completion.videoRequired
                        ? lesson.completion.videoCompleted
                          ? '✓ Đã xem hết video'
                          : '○ Xem hết video'
                        : '✓ Không có video'
                    }}</span
                  >
                  <span
                    class="rounded-lg border px-2.5 py-1.5"
                    :class="
                      lesson.completion.checkpointPassed === lesson.completion.checkpointTotal
                        ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
                        : 'border-[#edc2c7] bg-[#fff3f4] text-[#a0001c]'
                    "
                    >{{ lesson.completion.checkpointPassed }}/{{
                      lesson.completion.checkpointTotal
                    }}
                    checkpoint</span
                  >
                  <span
                    class="rounded-lg border px-2.5 py-1.5"
                    :class="
                      reinforcementThresholdMet
                        ? 'border-[#b9dfcc] bg-[#eef9f3] text-[#16764f]'
                        : 'border-[#edc2c7] bg-[#fff3f4] text-[#a0001c]'
                    "
                    >Điểm {{ reinforcementScoreLabel }}% / yêu cầu >
                    {{ lesson.completion.requiredScorePercent }}%</span
                  >
                </div>
              </div>
              <div class="shrink-0 text-right">
                <button
                  type="button"
                  :disabled="completing || isCompleted || !lesson.completion.canComplete"
                  class="h-10 rounded-lg bg-[#c9152b] px-5 text-sm font-bold text-white hover:bg-[#a0001c] disabled:cursor-not-allowed disabled:bg-[#b8afad]"
                  @click="complete"
                >
                  {{
                    isCompleted
                      ? 'Đã hoàn thành'
                      : completing
                        ? 'Đang cập nhật…'
                        : lesson.completion.canComplete
                          ? 'Hoàn thành bài học'
                          : 'Chưa đủ điều kiện'
                  }}
                </button>
                <p
                  v-if="!isCompleted && !lesson.completion.canComplete"
                  class="mt-1.5 max-w-[260px] text-[10px] leading-4 text-[#8d7c7a]"
                >
                  {{ nextRequirement }}
                </p>
              </div>
            </div>
          </section>

          <nav class="mt-5 flex justify-between gap-3 border-t border-[#e4dcdb] pt-5">
            <RouterLink
              v-if="lesson.previousLessonId"
              :to="`/student/learn/${lesson.previousLessonId}`"
              class="h-9 rounded-lg border border-[#ddd4d3] bg-white px-3 py-2 text-xs font-bold text-[#665957]"
              >← Bài trước</RouterLink
            ><span v-else />
            <RouterLink
              v-if="lesson.nextLessonId && isCompleted"
              :to="`/student/learn/${lesson.nextLessonId}`"
              class="h-9 rounded-lg bg-[#c9152b] px-3 py-2 text-xs font-bold text-white"
              >Bài tiếp theo →</RouterLink
            >
          </nav>
        </div>
      </main>

      <!-- Checkpoint blocks the learning flow until answered correctly. -->
      <div
        v-if="activeCheckpoint"
        class="fixed inset-0 z-[80] grid place-items-center bg-black/55 p-4 backdrop-blur-[2px]"
        role="dialog"
        aria-modal="true"
        aria-label="Câu hỏi trong video"
      >
        <div class="w-full max-w-xl rounded-2xl bg-[#f8f7f5] p-4 shadow-2xl sm:p-5">
          <div class="mb-3">
            <p class="text-[10px] font-black uppercase tracking-[0.14em] text-[#a0001c]">
              Checkpoint trong video ·
              {{ Math.floor((activeCheckpoint.videoTimestampSeconds ?? 0) / 60) }}:{{
                String((activeCheckpoint.videoTimestampSeconds ?? 0) % 60).padStart(2, '0')
              }}
            </p>
            <h2 class="mt-1 text-lg font-black text-[#302929]">Trả lời đúng để tiếp tục xem</h2>
          </div>
          <ExerciseCard :question="activeCheckpoint" @answered="onAnswered" />
          <p class="mt-3 text-center text-[10px] leading-4 text-[#81716f]">
            Video đang tạm dừng. Cửa sổ này chỉ đóng sau khi câu trả lời đúng.
          </p>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.lesson-content :deep(h1),
.lesson-content :deep(h2),
.lesson-content :deep(h3) {
  margin: 1.2rem 0 0.55rem;
  color: #302929;
  font-weight: 800;
  line-height: 1.35;
}
.lesson-content :deep(p) {
  margin: 0.65rem 0;
}
.lesson-content :deep(ul) {
  list-style: disc;
  padding-left: 1.4rem;
  margin: 0.7rem 0;
}
.lesson-content :deep(ol) {
  list-style: decimal;
  padding-left: 1.4rem;
  margin: 0.7rem 0;
}
.lesson-content :deep(blockquote) {
  margin: 0.9rem 0;
  border-left: 3px solid #c9152b;
  background: #faf5f5;
  padding: 0.7rem 0.9rem;
}
.lesson-content :deep(pre) {
  overflow-x: auto;
  border-radius: 0.65rem;
  background: #f3efee;
  padding: 0.85rem;
}
.lesson-content :deep(a) {
  color: #a0001c;
  font-weight: 700;
  text-decoration: underline;
}
</style>
