#!/usr/bin/env groovy

stage('Linux') {
    node('linux') {
        checkout scm
        sh "mono Protobuild.exe --automated-build"
    }
}


/*
stage 'Build'

def platforms = [:]

platforms['linux'] = {
}

platforms['mac'] = {
    node('mac') {
        git poll: false, url: 'https://github.com/Protobuild/Protobuild.IDE.MonoDevelop'
        //sh "/usr/local/bin/mono Protobuild.exe --automated-build"
    }
}

platforms['windows'] = {
    node('windows') {
        git poll: false, url: 'https://github.com/Protobuild/Protobuild.IDE.MonoDevelop'
        //bat 'Protobuild.exe --automated-build'
    }
}

parallel platforms
*/