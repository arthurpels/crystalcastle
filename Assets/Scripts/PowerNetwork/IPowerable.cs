public interface IPowerable {
  bool IsPowered { get; }
  void OnPowerChanged(bool powered);
}
