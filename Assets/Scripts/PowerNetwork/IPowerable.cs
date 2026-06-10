public interface IPowerable {
  bool IsPowered { get; }

  /// <param name="powered">Новое состояние питания.</param>
  /// <param name="force">
  /// true — применить состояние безусловно, даже если оно совпадает с текущим.
  /// Нужно для пере-синхронизации консьюмеров после загрузки/холодного старта,
  /// когда их рантайм-состояние сброшено, а нода не меняла значение.
  /// </param>
  void OnPowerChanged(bool powered, bool force = false);
}
