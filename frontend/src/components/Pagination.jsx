import './Pagination.css'

function Pagination({
  currentPage,
  itemLabel = 'kayıt',
  onPageChange,
  totalItems,
  totalPages,
}) {
  if (totalItems === 0) return null

  return (
    <nav className="data-pagination" aria-label={`${itemLabel} sayfaları`}>
      <span className="data-pagination-summary">
        Toplam {totalItems} {itemLabel} • Sayfa {currentPage} / {totalPages}
      </span>

      <div className="data-pagination-controls">
        <button
          type="button"
          className="data-pagination-button"
          onClick={() => onPageChange(page => page - 1)}
          disabled={currentPage === 1}
        >
          Önceki
        </button>

        {Array.from({ length: totalPages }, (_, index) => index + 1).map(pageNumber => (
          <button
            key={pageNumber}
            type="button"
            className={`data-pagination-button page-number${currentPage === pageNumber ? ' active' : ''}`}
            onClick={() => onPageChange(pageNumber)}
            aria-current={currentPage === pageNumber ? 'page' : undefined}
            aria-label={`${pageNumber}. sayfaya git`}
          >
            {pageNumber}
          </button>
        ))}

        <button
          type="button"
          className="data-pagination-button"
          onClick={() => onPageChange(page => page + 1)}
          disabled={currentPage === totalPages}
        >
          Sonraki
        </button>
      </div>
    </nav>
  )
}

export default Pagination
