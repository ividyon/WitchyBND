cmake_minimum_required(VERSION 3.20)
project(witchy_ooz LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN ON)

option(WITCHY_BUILD_OOZ_RESEARCH "Build the unsafe decoder research executable" OFF)

set(OOZ_SOURCES
  bitknit.cpp
  compr_entropy.cpp
  compr_kraken.cpp
  compr_leviathan.cpp
  compr_match_finder.cpp
  compr_mermaid.cpp
  compr_multiarray.cpp
  compr_tans.cpp
  compress.cpp
  kraken.cpp
  lzna.cpp
  stdafx.cpp)

add_library(witchy_ooz SHARED ${OOZ_SOURCES} witchy_ooz.cpp)

target_compile_definitions(witchy_ooz PRIVATE OOZ_BUILD_DLL=1 OOZ_DYNAMIC=1)
target_include_directories(witchy_ooz PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")

if(APPLE)
  target_compile_options(witchy_ooz PRIVATE -Wno-deprecated-declarations)
  target_link_options(witchy_ooz PRIVATE
    "LINKER:-exported_symbol,_WitchyOoz_Compress"
    "LINKER:-exported_symbol,_WitchyOoz_MaxCompressedSize")
  set_target_properties(witchy_ooz PROPERTIES
    OUTPUT_NAME witchy_ooz
    INSTALL_NAME_DIR "@rpath")
endif()

if(WITCHY_BUILD_OOZ_RESEARCH)
  add_executable(witchy_ooz_research ${OOZ_SOURCES})
  target_include_directories(witchy_ooz_research PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
  if(APPLE)
    target_compile_options(witchy_ooz_research PRIVATE -Wno-deprecated-declarations)
  endif()
endif()
