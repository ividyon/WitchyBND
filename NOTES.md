* Fixed crashes with parallelized PARAM (de)serialization by introducing a Preprocess step to parsers.
  * Preprocess will run some operations over each path in the file list for the sake of not having to do them again per file.
  * This is currently in use by WPARAM and WPARAMBND4 parsers.