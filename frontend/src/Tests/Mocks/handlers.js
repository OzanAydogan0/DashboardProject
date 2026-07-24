import { http, HttpResponse } from 'msw'

import { successfulLoginResponse } from '../fixtures/auth.js'

export const handlers = [
  http.post('http://localhost:5074/auth/login', async () => {
    return HttpResponse.json(successfulLoginResponse, {
      status: 200,
    })
  }),
]