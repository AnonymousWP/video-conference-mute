cmake_minimum_required(VERSION 3.20)
project(VcmVirtualCamera LANGUAGES CXX RC)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
# Static CRT: the DLL is loaded by the Frame Server service and must not depend on the VC++ redistributable.
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

set(WIL_INCLUDE_DIR "/opt/wil/include" CACHE PATH "Windows Implementation Libraries include directory")

add_library(VcmVirtualCamera SHARED
    Activate.cpp
    CameraReader.cpp
    CameraUtil.cpp
    Exports.cpp
    Log.cpp
    MediaSource.cpp
    MediaStream.cpp
    OverlayFrame.cpp
    PipeClient.cpp
    VcmVirtualCamera.def
    VcmVirtualCamera.rc)

set(VCM_VERSION "" CACHE STRING "Version x.y.z[-suffix] stamped into the DLL resources (required)")
string(REGEX MATCH "^([0-9]+)\\.([0-9]+)\\.([0-9]+)" _vcm_ver "${VCM_VERSION}")
if(NOT _vcm_ver)
    message(FATAL_ERROR "VCM_VERSION must start with x.y.z, got '${VCM_VERSION}'")
endif()
set_source_files_properties(VcmVirtualCamera.rc PROPERTIES COMPILE_DEFINITIONS
    "VCM_VERSION_MAJOR=${CMAKE_MATCH_1};VCM_VERSION_MINOR=${CMAKE_MATCH_2};VCM_VERSION_PATCH=${CMAKE_MATCH_3}")

target_include_directories(VcmVirtualCamera PRIVATE ${CMAKE_CURRENT_SOURCE_DIR} ${WIL_INCLUDE_DIR})
target_precompile_headers(VcmVirtualCamera PRIVATE pch.h)
target_compile_definitions(VcmVirtualCamera PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN NOMINMAX _WIN32_WINNT=0x0A00 NTDDI_VERSION=0x0A00000C)
target_compile_options(VcmVirtualCamera PRIVATE /W4 /permissive- /EHsc /Zc:__cplusplus /utf-8 /guard:cf /Qspectre- /wd4100 /wd4458 /wd4324)
target_link_options(VcmVirtualCamera PRIVATE /DELAYLOAD:mfsensorgroup.dll /GUARD:CF /DYNAMICBASE /NXCOMPAT)
target_link_libraries(VcmVirtualCamera PRIVATE
    mf mfplat mfreadwrite mfuuid mfsensorgroup
    ole32 oleaut32 propsys windowscodecs shell32 advapi32 delayimp runtimeobject)
