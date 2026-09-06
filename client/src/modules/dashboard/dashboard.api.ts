import { http } from '@/api/http'

import type { Dashboard } from './models/Dashboard'

export async function getDashboard(): Promise<Dashboard> {
  const response = await http.get<Dashboard>('/admin/dashboard')
  return response.data
}
