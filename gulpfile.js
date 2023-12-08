var gulp = require('gulp');
var browserify = require('browserify');
var source = require('vinyl-source-stream');
var tsify = require('tsify');
var uglify = require('gulp-uglify');
var sourcemaps = require('gulp-sourcemaps');
var buffer = require('vinyl-buffer');
var gts = require("gulp-typescript");

var tsProject = gts.createProject('tsconfig.json');

gulp.task('ts', function () {
    return browserify({
        basedir: '.',
        debug: true,
        entries: ['./main.ts'],
        cache: {},
        packageCache: {},
    })
        .plugin(tsify)
        .transform('babelify', {
            presets: ['es2015'],
            extensions: ['.ts']
        })
        .bundle()
        .pipe(source('miko.js'))
        .pipe(buffer())
        .pipe(sourcemaps.init({ loadMaps: true }))
        .pipe(uglify())
        .pipe(sourcemaps.write('./'))
        .pipe(gulp.dest('src/components/wwwroot/js'));
});

gulp.task('default', gulp.parallel('ts'), function () { });