interface Props {
  search: string
  genre: string
  genres: string[]
  onSearchChange: (value: string) => void
  onGenreChange: (value: string) => void
}

export default function Toolbar({
  search,
  genre,
  genres,
  onSearchChange,
  onGenreChange,
}: Props) {
  return (
    <div className="toolbar">
      <input
        className="search"
        type="search"
        value={search}
        onChange={(e) => onSearchChange(e.target.value)}
        placeholder="Поиск по названию или автору"
      />

      <select value={genre} onChange={(e) => onGenreChange(e.target.value)}>
        <option value="">Все жанры</option>
        {genres.map((g) => (
          <option key={g} value={g}>
            {g}
          </option>
        ))}
      </select>
    </div>
  )
}
