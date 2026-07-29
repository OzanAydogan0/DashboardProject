import { useState } from 'react'

export const DEFAULT_PAGE_SIZE = 10

export const usePagination = (items, pageSize = DEFAULT_PAGE_SIZE) => {
  const [selectedPage, setSelectedPage] = useState(1)
  const safeItems = Array.isArray(items) ? items : []
  const totalItems = safeItems.length
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize))
  const currentPage = Math.min(selectedPage, totalPages)
  const startIndex = (currentPage - 1) * pageSize
  const paginatedItems = safeItems.slice(startIndex, startIndex + pageSize)

  const setCurrentPage = (nextPage) => {
    setSelectedPage((previousPage) => {
      const safePreviousPage = Math.min(previousPage, totalPages)
      const resolvedPage = typeof nextPage === 'function'
        ? nextPage(safePreviousPage)
        : nextPage

      return Math.min(totalPages, Math.max(1, Number(resolvedPage) || 1))
    })
  }

  return {
    currentPage,
    pageSize,
    paginatedItems,
    setCurrentPage,
    totalItems,
    totalPages,
  }
}
