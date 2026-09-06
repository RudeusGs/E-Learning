<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const props = defineProps<{
  open: boolean
  role: 'ADMIN' | 'STUDENT'
}>()

const emit = defineEmits<{
  close: []
}>()

const route = useRoute()

interface GuideContent {
  eyebrow: string
  title: string
  goal: string
  steps: string[]
  tip: string
}

const guide = computed<GuideContent>(() => {
  const path = route.path

  if (props.role === 'ADMIN') {
    if (/^\/admin\/lessons\/[^/]+\/exercises$/.test(path)) {
      return {
        eyebrow: 'Câu hỏi & kiểm tra',
        title: 'Chọn đúng loại câu hỏi trước',
        goal: 'Dùng checkpoint để kiểm tra ngay trong video; dùng củng cố để kiểm tra sau khi xem xong.',
        steps: [
          'Checkpoint trong video: chọn mốc thời gian, học viên phải trả lời đúng mới xem tiếp.',
          'Củng cố cuối bài: chỉ mở sau khi video kết thúc.',
          'Học viên phải đạt trên 80% câu củng cố mới được hoàn thành bài.',
        ],
        tip: 'Nếu không chọn được checkpoint, hãy quay lại Chỉnh sửa bài học và nhập thời lượng video tin cậy.',
      }
    }

    if (/^\/admin\/lessons\/[^/]+\/edit$/.test(path)) {
      return {
        eyebrow: 'Thiết lập bài học',
        title: 'Làm theo thứ tự này',
        goal: 'Hoàn thiện nội dung trước, sau đó cấu hình video và câu hỏi rồi mới xuất bản.',
        steps: [
          'Nhập tiêu đề, mô tả và nội dung bài.',
          'Nếu có video: nhập URL YouTube và thời lượng tin cậy.',
          'Mở “Câu hỏi & kiểm tra” để thêm checkpoint hoặc bài củng cố.',
          'Chuyển trạng thái sang Đã xuất bản khi nội dung đã sẵn sàng.',
        ],
        tip: 'Không cần xuất bản ngay. Lưu ở Bản nháp trong lúc đang biên soạn sẽ an toàn hơn.',
      }
    }

    if (/^\/admin\/courses\/[^/]+$/.test(path)) {
      return {
        eyebrow: 'Thiết lập khóa học',
        title: 'Từ khóa học đến học viên',
        goal: 'Khóa học chỉ dễ vận hành khi bạn đi theo một chuỗi cố định.',
        steps: [
          'Kiểm tra tên, mô tả và ảnh đại diện.',
          'Thêm các bài học theo đúng thứ tự.',
          'Hoàn thiện video, checkpoint, bài củng cố rồi xuất bản lesson.',
          'Xuất bản course và gán course cho học viên.',
        ],
        tip: 'Nếu đang phân vân bước tiếp theo, dùng khối “Bước tiếp theo” ngay trong trang khóa học.',
      }
    }

    if (path === '/admin/courses/create') {
      return {
        eyebrow: 'Tạo khóa học',
        title: 'Bắt đầu đơn giản',
        goal: 'Tạo khung khóa học trước, nội dung chi tiết được xây dựng ở các bước sau.',
        steps: [
          'Đặt tên và mô tả dễ nhận biết.',
          'Giữ Bản nháp nếu khóa học chưa có đủ bài.',
          'Sau khi tạo, thêm lesson trong trang chi tiết khóa học.',
        ],
        tip: 'Thứ tự hiển thị chỉ ảnh hưởng vị trí khóa học trong danh sách; số nhỏ hơn đứng trước.',
      }
    }

    if (/^\/admin\/courses\/[^/]+\/lessons\/create$/.test(path)) {
      return {
        eyebrow: 'Tạo bài học',
        title: 'Tạo khung, rồi hoàn thiện',
        goal: 'Bạn có thể tạo bài nháp trước rồi quay lại bổ sung video, nội dung và câu hỏi.',
        steps: [
          'Nhập tiêu đề và thứ tự lesson.',
          'Thêm video nếu cần và nhập đúng thời lượng.',
          'Lưu bài học.',
          'Sang “Câu hỏi & kiểm tra” để tạo checkpoint và bài củng cố.',
        ],
        tip: 'Nếu đang soạn dở, chọn Bản nháp để học viên chưa nhìn thấy lesson.',
      }
    }

    if (path.startsWith('/admin/students')) {
      return {
        eyebrow: 'Quản lý học viên',
        title: 'Tạo tài khoản rồi gán khóa học',
        goal: 'Học viên chỉ thấy khóa học khi có enrollment đang Active.',
        steps: [
          'Tạo học viên.',
          'Mở Chi tiết học viên.',
          'Gán khóa học phù hợp.',
          'Theo dõi kết quả ở Tiến độ học tập.',
        ],
        tip: 'Vô hiệu hóa tài khoản sẽ chặn đăng nhập; contract hiện tại chưa có thao tác kích hoạt lại.',
      }
    }

    if (path.startsWith('/admin/progress')) {
      return {
        eyebrow: 'Theo dõi tiến độ',
        title: 'Tìm đúng người, đúng khóa',
        goal: 'Dùng search và bộ lọc server-side để kiểm tra tình trạng học thật.',
        steps: [
          'Tìm theo tên học viên hoặc khóa học.',
          'Lọc thêm bằng ID khi cần đối chiếu chính xác.',
          'Mở Chi tiết học viên hoặc khóa học từ từng dòng.',
        ],
        tip: 'Bảng dùng cursor pagination nên nút “Tải thêm” là hành vi đúng, không phải thiếu số trang.',
      }
    }

    return {
      eyebrow: 'Quản trị',
      title: 'Luồng vận hành chuẩn',
      goal: 'Nếu mới dùng hệ thống, đi theo đúng chuỗi dưới đây để không bỏ sót bước.',
      steps: [
        'Tạo khóa học.',
        'Thêm và hoàn thiện bài học.',
        'Tạo checkpoint / bài củng cố.',
        'Xuất bản nội dung.',
        'Tạo học viên và gán khóa học.',
        'Theo dõi tiến độ.',
      ],
      tip: 'Mỗi màn hình đều có một hành động chính. Ưu tiên nút đỏ trước, các nút viền là hành động phụ.',
    }
  }

  if (path.startsWith('/student/learn')) {
    return {
      eyebrow: 'Đang học',
      title: 'Muốn hoàn thành bài này?',
      goal: 'Không chỉ cần mở bài. Hệ thống kiểm tra đủ quá trình học trước khi mở nút hoàn thành.',
      steps: [
        'Xem video theo thứ tự; không thể tua tới phần chưa xem.',
        'Checkpoint sẽ tự bật đúng mốc và phải trả lời đúng mới xem tiếp.',
        'Sau video, làm bài củng cố và đạt trên 80%.',
        'Khi mọi điều kiện chuyển xanh, bấm Hoàn thành bài học.',
      ],
      tip: 'Chuyển tab hoặc đổi cửa sổ sẽ tự dừng video. Bạn vẫn có thể tua lùi để xem lại.',
    }
  }

  if (/^\/student\/courses\/[^/]+\/progress$/.test(path)) {
    return {
      eyebrow: 'Tiến độ',
      title: 'Đọc lộ trình như thế nào?',
      goal: 'Mỗi lesson có trạng thái riêng và chỉ mở khi lesson trước đã hoàn thành.',
      steps: [
        'Đã hoàn thành: có thể học lại.',
        'Đang học / Sẵn sàng: có thể truy cập ngay.',
        'Chưa mở khóa: hoàn thành bài đứng trước để tiếp tục.',
      ],
      tip: 'Phần trăm tiến độ tính từ các lesson Published đã hoàn thành.',
    }
  }

  if (/^\/student\/courses\/[^/]+$/.test(path)) {
    return {
      eyebrow: 'Khóa học',
      title: 'Bắt đầu từ bài đang mở',
      goal: 'Không cần tự tìm lesson tiếp theo; hệ thống đã đánh dấu bài có thể học.',
      steps: [
        'Bấm Bắt đầu học hoặc Tiếp tục học.',
        'Hoàn thành lesson hiện tại.',
        'Lesson kế tiếp sẽ tự mở theo learning path.',
      ],
      tip: 'Bài có biểu tượng khóa chưa thể truy cập dù bạn đổi URL thủ công.',
    }
  }

  return {
    eyebrow: 'Học viên',
    title: 'Cách học nhanh nhất',
    goal: 'Chọn khóa học đang học và tiếp tục từ lesson hệ thống gợi ý.',
    steps: [
      'Mở Khóa học của tôi.',
      'Chọn khóa đang học.',
      'Tiếp tục lesson hiện tại.',
      'Hoàn thành từng lesson để mở khóa bài tiếp theo.',
    ],
    tip: 'Bạn có thể xem Tiến độ khóa học để biết chính xác bài nào đã hoàn thành và bài nào đang khóa.',
  }
})
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-[120]">
      <button
        type="button"
        class="absolute inset-0 bg-black/35 backdrop-blur-[1px]"
        aria-label="Đóng hướng dẫn"
        @click="emit('close')"
      />

      <aside
        class="absolute inset-y-0 right-0 flex w-[min(390px,92vw)] flex-col border-l border-[#e5dedd] bg-[#f8f7f5] shadow-2xl"
        role="dialog"
        aria-modal="true"
        aria-label="Hướng dẫn theo ngữ cảnh"
      >
        <header class="flex items-start justify-between gap-4 border-b border-[#e5dedd] bg-white px-5 py-5">
          <div>
            <p class="text-[10px] font-black uppercase tracking-[0.16em] text-[#a0001c]">
              {{ guide.eyebrow }}
            </p>
            <h2 class="mt-1 text-lg font-black tracking-[-0.02em] text-[#302929]">
              {{ guide.title }}
            </h2>
          </div>

          <button
            type="button"
            class="grid size-9 shrink-0 place-items-center rounded-lg border border-[#e5dedd] bg-white text-[#756765] hover:bg-[#f5f1f0] hover:text-[#a0001c]"
            aria-label="Đóng hướng dẫn"
            @click="emit('close')"
          >
            <svg class="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 6l12 12M18 6 6 18" />
            </svg>
          </button>
        </header>

        <div class="flex-1 overflow-y-auto px-5 py-5">
          <p class="rounded-xl border border-[#eadfdf] bg-white px-4 py-3 text-sm leading-6 text-[#665957]">
            {{ guide.goal }}
          </p>

          <ol class="mt-6 space-y-4">
            <li v-for="(step, index) in guide.steps" :key="step" class="flex gap-3">
              <span
                class="grid size-7 shrink-0 place-items-center rounded-full bg-[#fff0f1] text-[11px] font-black text-[#a0001c]"
              >
                {{ index + 1 }}
              </span>
              <p class="pt-1 text-sm leading-5 text-[#504644]">{{ step }}</p>
            </li>
          </ol>

          <div class="mt-7 flex gap-3 rounded-xl border border-[#efd8b9] bg-[#fff8ed] p-4">
            <svg
              class="mt-0.5 size-4.5 shrink-0 text-[#a66315]"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.9"
            >
              <path d="M12 3a7 7 0 0 0-4 12.74V19h8v-3.26A7 7 0 0 0 12 3Z" />
              <path d="M9 22h6M9 16h6" />
            </svg>
            <div>
              <p class="text-xs font-black text-[#7a592c]">Mẹo sử dụng</p>
              <p class="mt-1 text-xs leading-5 text-[#876736]">{{ guide.tip }}</p>
            </div>
          </div>
        </div>

        <footer class="border-t border-[#e5dedd] bg-white px-5 py-4">
          <button
            type="button"
            class="h-10 w-full rounded-lg bg-[#c9152b] px-4 text-sm font-bold text-white hover:bg-[#a0001c]"
            @click="emit('close')"
          >
            Đã hiểu
          </button>
        </footer>
      </aside>
    </div>
  </Teleport>
</template>
