class StringReader {
  String source;
  int _position = -1;
  int _markPosition = 0;

  StringReader(this.source);

  bool next() {
    return ++_position < source.length;
  }

  void mark() {
    _markPosition = _position;
  }

  String get() {
    return source.substring(_markPosition, _position + 1);
  }

  String getInnerContent() {
    return source.substring(_markPosition + 1, _position);
  }

  void seek_until_in(set) {
    while (_position + 1 < source.length && set.contains(source[_position + 1]))
      ++_position;
  }

  void seek_until(ch) {
    while (_position + 1 < source.length && source[_position + 1] != ch)
      ++_position;
  }

  bool current_in(set) {
    return _position < source.length && set.contains(source[_position]);
  }

  bool current_in_map(set) {
    return _position < source.length && set.containsKey(source[_position]);
  }

  bool current_is(ch) {
    return _position < source.length && source[_position] == ch;
  }

  bool peek_in(set) {
    return _position + 1 < source.length && set.contains(source[_position + 1]);
  }

  bool peek_is(ch) {
    return _position + 1 < source.length && source[_position + 1] == ch;
  }
}
