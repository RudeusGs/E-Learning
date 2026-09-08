<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{
  modelValue: number | null
  disabled?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: number | null): void
}>()

const hh = ref('')
const mm = ref('')
const ss = ref('')

const hhRef = ref<HTMLInputElement | null>(null)
const mmRef = ref<HTMLInputElement | null>(null)
const ssRef = ref<HTMLInputElement | null>(null)

// Watch for external changes only (like initial load or programmatic clear)
watch(
  () => props.modelValue,
  (newVal) => {
    const currentCalculated =
      parseInt(hh.value || '0', 10) * 3600 +
      parseInt(mm.value || '0', 10) * 60 +
      parseInt(ss.value || '0', 10)

    if (newVal === currentCalculated && (newVal !== 0 || hh.value || mm.value || ss.value)) {
      return // Same value, don't override local typing state
    }

    if (newVal === null || newVal === undefined) {
      hh.value = ''
      mm.value = ''
      ss.value = ''
    } else {
      const h = Math.floor(newVal / 3600)
      const m = Math.floor((newVal % 3600) / 60)
      const s = newVal % 60

      hh.value = h > 0 ? h.toString().padStart(2, '0') : ''
      mm.value = m.toString().padStart(2, '0')
      ss.value = s.toString().padStart(2, '0')
    }
  },
  { immediate: true },
)

function updateValue() {
  const h = parseInt(hh.value || '0', 10)
  const m = parseInt(mm.value || '0', 10)
  const s = parseInt(ss.value || '0', 10)

  if (!hh.value && !mm.value && !ss.value) {
    emit('update:modelValue', null)
    return
  }

  emit('update:modelValue', h * 3600 + m * 60 + s)
}

function handleInput(type: 'hh' | 'mm' | 'ss', event: Event) {
  const target = event.target as HTMLInputElement
  let val = target.value.replace(/\D/g, '')

  if (type === 'hh') {
    let num = parseInt(val, 10)
    if (!isNaN(num) && num > 5) {
      val = '05'
    }
    hh.value = val
    if (val.length >= 2) {
      mmRef.value?.focus()
    }
  } else if (type === 'mm') {
    let num = parseInt(val, 10)
    if (!isNaN(num) && num > 59) {
      val = '59'
    }
    mm.value = val
    if (val.length >= 2) {
      ssRef.value?.focus()
    }
  } else {
    let num = parseInt(val, 10)
    if (!isNaN(num) && num > 59) {
      val = '59'
    }
    ss.value = val
  }

  target.value = val
  updateValue()
}

function handleBlur(type: 'hh' | 'mm' | 'ss') {
  if (type === 'hh' && hh.value.length === 1) hh.value = hh.value.padStart(2, '0')
  if (type === 'mm' && mm.value.length === 1) mm.value = mm.value.padStart(2, '0')
  if (type === 'ss' && ss.value.length === 1) ss.value = ss.value.padStart(2, '0')
  updateValue()
}
</script>

<template>
  <div
    class="inline-flex h-10 items-center rounded-lg border border-[#ddd4d3] bg-white px-3 focus-within:border-[#c9152b] focus-within:ring-3 focus-within:ring-[#c9152b]/10"
    :class="{ 'opacity-60 bg-[#f4f1f0]': disabled }"
  >
    <input
      ref="hhRef"
      :value="hh"
      type="text"
      inputmode="numeric"
      placeholder="HH"
      :disabled="disabled"
      class="w-6 bg-transparent p-0 text-center text-sm font-bold text-[#302929] outline-none placeholder:text-[#c4b9b7] disabled:cursor-not-allowed"
      @input="handleInput('hh', $event)"
      @blur="handleBlur('hh')"
    />
    <span class="mx-1 text-sm font-bold text-[#c4b9b7]">:</span>
    <input
      ref="mmRef"
      :value="mm"
      type="text"
      inputmode="numeric"
      placeholder="MM"
      :disabled="disabled"
      class="w-6 bg-transparent p-0 text-center text-sm font-bold text-[#302929] outline-none placeholder:text-[#c4b9b7] disabled:cursor-not-allowed"
      @input="handleInput('mm', $event)"
      @blur="handleBlur('mm')"
    />
    <span class="mx-1 text-sm font-bold text-[#c4b9b7]">:</span>
    <input
      ref="ssRef"
      :value="ss"
      type="text"
      inputmode="numeric"
      placeholder="SS"
      :disabled="disabled"
      class="w-6 bg-transparent p-0 text-center text-sm font-bold text-[#302929] outline-none placeholder:text-[#c4b9b7] disabled:cursor-not-allowed"
      @input="handleInput('ss', $event)"
      @blur="handleBlur('ss')"
    />
  </div>
</template>
