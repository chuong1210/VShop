from flask import Blueprint, render_template, request, jsonify
from werkzeug.utils import secure_filename
import os
from config.config import Config
from services.tools import add_document_to_knowledge_base, invalidate_product_cache
import json

admin_bp = Blueprint('admin', __name__, url_prefix='/admin')

ALLOWED_EXTENSIONS = {'docx', 'txt'}

def allowed_file(filename):
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS

@admin_bp.route('/')
def index():
    """Trang admin"""
    return render_template('admin.html')

@admin_bp.route('/upload', methods=['POST'])
def upload_document():
    """Upload và xử lý tài liệu"""
    if 'file' not in request.files:
        return jsonify({'success': False, 'message': 'Không có file nào được chọn'}), 400
    
    file = request.files['file']
    
    if file.filename == '':
        return jsonify({'success': False, 'message': 'Không có file nào được chọn'}), 400
    
    if file and allowed_file(file.filename):
        try:
            filename = secure_filename(file.filename)
            
            # Tạo thư mục uploads nếu chưa tồn tại
            os.makedirs(Config.UPLOAD_FOLDER, exist_ok=True)
            
            filepath = os.path.join(Config.UPLOAD_FOLDER, filename)
            file.save(filepath)
            
            # Metadata từ form
            doc_type = request.form.get('doc_type', 'general')
            description = request.form.get('description', '')
            
            # Gọi tool add_document_to_knowledge_base
            result_str = add_document_to_knowledge_base.invoke({
                "file_path": filepath,
                "doc_type": doc_type,
                "description": description
            })
            
            result = json.loads(result_str)
            
            if result.get('success'):
                return jsonify({
                    'success': True,
                    'message': result.get('message', 'Upload thành công!'),
                    'chunks_count': result.get('chunks_count', 0)
                })
            else:
                return jsonify({
                    'success': False,
                    'message': result.get('error', 'Có lỗi khi xử lý file')
                }), 500
                
        except Exception as e:
            print(f"Error uploading document: {e}")
            import traceback
            traceback.print_exc()
            return jsonify({
                'success': False,
                'message': f'Lỗi: {str(e)}'
            }), 500
    
    return jsonify({'success': False, 'message': 'File không hợp lệ'}), 400

@admin_bp.route('/refresh-products', methods=['POST'])
def refresh_products():
    """Làm mới thông tin sản phẩm trong RAG (invalidate cache)"""
    try:
        # Gọi tool invalidate_product_cache
        result_str = invalidate_product_cache.invoke({})
        result = json.loads(result_str)
        
        if result.get('success'):
            return jsonify({
                'success': True,
                'message': result.get('message', 'Đã làm mới cache sản phẩm!')
            })
        else:
            return jsonify({
                'success': False,
                'message': result.get('error', 'Có lỗi khi làm mới cache')
            }), 500
            
    except Exception as e:
        print(f"Error refreshing products: {e}")
        import traceback
        traceback.print_exc()
        return jsonify({
            'success': False,
            'message': f'Lỗi: {str(e)}'
        }), 500

@admin_bp.route('/stats', methods=['GET'])
def get_stats():
    """Lấy thống kê hệ thống"""
    try:
        from services.tools import ALL_TOOLS
        
        stats = {
            'available_tools': len(ALL_TOOLS),
            'tools': [
                {
                    'name': tool.name,
                    'description': tool.description[:100] + '...' if len(tool.description) > 100 else tool.description
                }
                for tool in ALL_TOOLS
            ]
        }
        
        # Lấy document count từ Elasticsearch (nếu cần)
        try:
            from services.tools import vector_store
            count = vector_store.client.count(index=Config.ES_INDEX_NAME)
            stats['document_count'] = count.get('count', 0)
        except:
            stats['document_count'] = 0
        
        return jsonify(stats)
        
    except Exception as e:
        print(f"Error getting stats: {e}")
        return jsonify({'error': str(e)}), 500